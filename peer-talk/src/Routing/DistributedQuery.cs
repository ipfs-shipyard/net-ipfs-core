using Common.Logging;
using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    /// <summary>
    ///   A query that is sent to multiple peers.
    /// </summary>
    /// <typeparam name="T">
    ///  The type of answer returned by a peer.
    /// </typeparam>
    public class DistributedQuery<T> where T : class
    {
        static ILog log = LogManager.GetLogger("PeerTalk.Routing.DistributedQuery");
        static int nextQueryId = 1;

        /// <summary>
        ///   The maximum number of peers that can be queried at one time
        ///   for all distributed queries.
        /// </summary>
        static SemaphoreSlim askCount = new SemaphoreSlim(128);

        /// <summary>
        ///   The maximum time spent on waiting for an answer from a peer.
        /// </summary>
        static readonly TimeSpan askTime = TimeSpan.FromSeconds(10);

        /// <summary>
        ///   Controls the running of the distributed query.
        /// </summary>
        /// <remarks>
        ///   Becomes cancelled when the correct number of answers are found
        ///   or the caller of <see cref="RunAsync"/> wants to cancel
        ///   or the DHT is stopped.
        /// </remarks>
        CancellationTokenSource runningQuery;

        List<Peer> visited = new List<Peer>();
        DhtMessage queryMessage;
        int failedConnects = 0;

        /// <summary>
        ///   Raised when an answer is obtained.
        /// </summary>
        public event EventHandler<T> AnswerObtained;

        /// <summary>
        ///   The unique identifier of the query.
        /// </summary>
        public int Id { get; } = nextQueryId++;

        /// <summary>
        ///   The received answers for the query.
        /// </summary>
        public List<T> Answers { get; } = new List<T>();

        /// <summary>
        ///   The number of answers needed.
        /// </summary>
        /// <remarks>
        ///   When the numbers <see cref="Answers"/> reaches this limit
        ///   the <see cref="RunAsync">running query</see> will stop.
        /// </remarks>
        public int AnswersNeeded { get; set; } = 1;

        /// <summary>
        ///   The maximum number of concurrent peer queries to perform
        ///   for one distributed query.
        /// </summary>
        /// <value>
        ///   The default is 16.
        /// </value>
        /// <remarks>
        ///   The number of peers that are asked for the answer.
        /// </remarks>
        public int ConcurrencyLevel { get; set; } = 16;

        /// <summary>
        ///   The distributed hash table.
        /// </summary>
        public Dht1 Dht { get; set; }

        /// <summary>
        ///   The type of query to perform.
        /// </summary>
        public MessageType QueryType { get; set; }

        /// <summary>
        ///   The key to find.
        /// </summary>
        public MultiHash QueryKey { get; set; }

        /// <summary>
        ///   Starts the distributed query.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public async Task RunAsync(CancellationToken cancel)
        {
            log.Debug($"Q{Id} run {QueryType} {QueryKey}");

            runningQuery = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            Dht.Stopped += OnDhtStopped;
            queryMessage = new DhtMessage
            {
                Type = QueryType,
                Key = QueryKey?.ToArray(),
            };

            var tasks = Enumerable
                .Range(1, ConcurrencyLevel)
                .Select(i => { var id = i; return AskAsync(id); });
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // eat it
            }
            finally
            {
                Dht.Stopped -= OnDhtStopped;
            }
            log.Debug($"Q{Id} found {Answers.Count} answers, visited {visited.Count} peers, failed {failedConnects}");
        }

        private void OnDhtStopped(object sender, EventArgs e)
        {
            log.Debug($"Q{Id} cancelled because DHT stopped.");
            runningQuery.Cancel();
        }

        /// <summary>
        ///   Ask the next peer the question.
        /// </summary>
        async Task AskAsync(int taskId)
        {
            int pass = 0;
            int waits = 20;
            while (!runningQuery.IsCancellationRequested && waits > 0)
            {
                // Get the nearest peer that has not been visited.
                var peer = Dht.RoutingTable
                    .NearestPeers(QueryKey)
                    .Where(p => !visited.Contains(p))
                    .FirstOrDefault();
                if (peer == null)
                {
                    --waits;
                    await Task.Delay(100);
                    continue;
                }

                ++pass;
                visited.Add(peer);

                // Ask the nearest peer.
                await askCount.WaitAsync(runningQuery.Token).ConfigureAwait(false);
                var start = DateTime.Now;
                log.Debug($"Q{Id}.{taskId}.{pass} ask {peer}");
                try
                {
                    using (var timeout = new CancellationTokenSource(askTime))
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, runningQuery.Token))
                    using (var stream = await Dht.Swarm.DialAsync(peer, Dht.ToString(), cts.Token).ConfigureAwait(false))
                    {
                        // Send the KAD query and get a response.
                        ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, queryMessage, PrefixStyle.Base128);
                        await stream.FlushAsync(cts.Token).ConfigureAwait(false);
                        var response = await ProtoBufHelper.ReadMessageAsync<DhtMessage>(stream, cts.Token).ConfigureAwait(false);

                        // Process answer
                        ProcessProviders(response.ProviderPeers);
                        ProcessCloserPeers(response.CloserPeers);
                    }
                    var time = DateTime.Now - start;
                    log.Debug($"Q{Id}.{taskId}.{pass} ok {peer} ({time.TotalMilliseconds} ms)");
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref failedConnects);
                    var time = DateTime.Now - start;
                    log.Warn($"Q{Id}.{taskId}.{pass} failed ({time.TotalMilliseconds} ms) - {e.Message}");
                    // eat it
                }
                finally
                {
                    askCount.Release();
                }
            }
        }

        void ProcessProviders(DhtPeerMessage[] providers)
        {
            if (providers == null)
                return;

            foreach (var provider in providers)
            {
                if (provider.TryToPeer(out Peer p))
                {
                    if (p == Dht.Swarm.LocalPeer)
                        continue;

                    p = Dht.Swarm.RegisterPeer(p);
                    if (QueryType == MessageType.GetProviders)
                    {
                        // Only unique answers
                        var answer = p as T;
                        if (!Answers.Contains(answer))
                        {
                            AddAnswer(answer);
                        }
                    }
                }
            }
        }

        void ProcessCloserPeers(DhtPeerMessage[] closerPeers)
        {
            if (closerPeers == null)
                return;
            foreach (var closer in closerPeers)
            {
                if (closer.TryToPeer(out Peer p))
                {
                    if (p == Dht.Swarm.LocalPeer)
                        continue;

                    p = Dht.Swarm.RegisterPeer(p);
                    if (QueryType == MessageType.FindNode && QueryKey == p.Id)
                    {
                        AddAnswer(p as T);
                    }
                }
            }
        }

        void AddAnswer(T answer)
        {
            if (answer == null || runningQuery.IsCancellationRequested)
                return;

            Answers.Add(answer);
            if (Answers.Count >= AnswersNeeded && !runningQuery.IsCancellationRequested)
            {
                runningQuery.Cancel(false);
            }

            AnswerObtained?.Invoke(this, answer);
        }
    }
}
