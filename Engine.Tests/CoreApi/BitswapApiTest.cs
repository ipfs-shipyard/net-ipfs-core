using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Engine.BlockExchange;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class BitswapApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;
    private readonly IpfsEngine _ipfsOther = TestFixture.IpfsOther;

    [TestMethod]
    public async Task Wants()
    {
        await _ipfs.StartAsync();
        try
        {
            var cts = new CancellationTokenSource();
            var block = new DagNode("BitswapApiTest unknown block"u8.ToArray());
            Task wantTask = _ipfs.Bitswap.GetAsync(block.Id, cts.Token);

            var endTime = DateTime.Now.AddSeconds(10);
            while (true)
            {
                if (DateTime.Now > endTime)
                {
                    Assert.Fail("wanted block is missing");
                }

                await Task.Delay(100, cts.Token);
                var w = await _ipfs.Bitswap.WantsAsync(cancel: cts.Token);
                if (w.Contains(block.Id))
                {
                    break;
                }
            }

            cts.Cancel();
            var wants = await _ipfs.Bitswap.WantsAsync(cancel: cts.Token);
            CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
            Assert.IsTrue(wantTask.IsCanceled);
        }
        finally
        {
            await _ipfs.StopAsync();
        }
    }

    [TestMethod]
    public async Task Unwant()
    {
        await _ipfs.StartAsync();
        try
        {
            var block = new DagNode("BitswapApiTest unknown block 2"u8.ToArray());
            Task wantTask = _ipfs.Bitswap.GetAsync(block.Id);

            var endTime = DateTime.Now.AddSeconds(10);
            while (true)
            {
                if (DateTime.Now > endTime)
                {
                    Assert.Fail("wanted block is missing");
                }

                await Task.Delay(100);
                var w = await _ipfs.Bitswap.WantsAsync();
                if (w.Contains(block.Id))
                {
                    break;
                }
            }

            await _ipfs.Bitswap.UnwantAsync(block.Id);
            var wants = await _ipfs.Bitswap.WantsAsync();
            CollectionAssert.DoesNotContain(wants.ToArray(), block.Id);
            Assert.IsTrue(wantTask.IsCanceled);
        }
        finally
        {
            await _ipfs.StopAsync();
        }
    }

    [TestMethod]
    public async Task OnConnect_Sends_WantList()
    {
        _ipfs.Options.Discovery.DisableMdns = true;
        _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfs.StartAsync();

        _ipfsOther.Options.Discovery.DisableMdns = true;
        _ipfsOther.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfsOther.StartAsync();
        try
        {
            var local = await _ipfs.LocalPeer;
            var remote = await _ipfsOther.LocalPeer;
            Console.WriteLine($"this at {local.Addresses.First()}");
            Console.WriteLine($"othr at {remote.Addresses.First()}");

            var data = Guid.NewGuid().ToByteArray();
            var cid = new Cid { Hash = MultiHash.ComputeHash(data) };
            var _ = _ipfs.Block.GetAsync(cid);
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First());

            var endTime = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < endTime)
            {
                var wants = await _ipfsOther.Bitswap.WantsAsync(local.Id);
                if (wants.Contains(cid))
                {
                    return;
                }

                await Task.Delay(200);
            }

            Assert.Fail("want list not sent");
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();

            _ipfs.Options.Discovery = new();
            _ipfsOther.Options.Discovery = new();
        }
    }

    [TestMethod]
    public async Task GetsBlock_OnConnect()
    {
        _ipfs.Options.Discovery.DisableMdns = true;
        _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfs.StartAsync();

        _ipfsOther.Options.Discovery.DisableMdns = true;
        _ipfsOther.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfsOther.StartAsync();
        try
        {
            var data = Guid.NewGuid().ToByteArray();
            var cid = await _ipfsOther.Block.PutAsync(data);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var getTask = _ipfs.Block.GetAsync(cid, cts.Token);

            var remote = await _ipfsOther.LocalPeer;
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
            var block = await getTask;

            Assert.IsFalse(getTask.IsCanceled, "task cancelled");
            Assert.IsFalse(getTask.IsFaulted, "task faulted");
            Assert.IsTrue(getTask.IsCompleted, "task not completed");
            Assert.AreEqual(cid, block.Id);
            CollectionAssert.AreEqual(data, block.DataBytes);

            var otherPeer = await _ipfsOther.LocalPeer;
            var ledger = await _ipfs.Bitswap.LedgerAsync(otherPeer, cts.Token);
            Assert.AreEqual(otherPeer, ledger.Peer);
            Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
            Assert.AreNotEqual(0UL, ledger.DataReceived);
            Assert.AreEqual(0UL, ledger.DataSent);
            Assert.IsTrue(ledger.IsInDebt);

            // TODO: Timing issue here.  ipfsOther could have sent the block
            // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();

            _ipfs.Options.Discovery = new();
            _ipfsOther.Options.Discovery = new();
        }
    }

    [TestMethod]
    public async Task GetsBlock_OnConnect_Bitswap1()
    {
        var originalProtocols = (await _ipfs.BitswapService).Protocols;
        var otherOriginalProtocols = (await _ipfsOther.BitswapService).Protocols;

        (await _ipfs.BitswapService).Protocols = new IBitswapProtocol[]
        {
            new Bitswap1 { Bitswap = await _ipfs.BitswapService }
        };
        _ipfs.Options.Discovery.DisableMdns = true;
        _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfs.StartAsync();

        (await _ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
        {
            new Bitswap1 { Bitswap = await _ipfsOther.BitswapService }
        };
        _ipfsOther.Options.Discovery.DisableMdns = true;
        _ipfsOther.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfsOther.StartAsync();
        try
        {
            var data = Guid.NewGuid().ToByteArray();
            var cid = await _ipfsOther.Block.PutAsync(data);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var getTask = _ipfs.Block.GetAsync(cid, cts.Token);

            var remote = await _ipfsOther.LocalPeer;
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
            var block = await getTask;

            Assert.IsFalse(getTask.IsCanceled, "task cancelled");
            Assert.IsFalse(getTask.IsFaulted, "task faulted");
            Assert.IsTrue(getTask.IsCompleted, "task not completed");
            Assert.AreEqual(cid, block.Id);
            CollectionAssert.AreEqual(data, block.DataBytes);

            var otherPeer = await _ipfsOther.LocalPeer;
            var ledger = await _ipfs.Bitswap.LedgerAsync(otherPeer, cts.Token);
            Assert.AreEqual(otherPeer, ledger.Peer);
            Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
            Assert.AreNotEqual(0UL, ledger.DataReceived);
            Assert.AreEqual(0UL, ledger.DataSent);
            Assert.IsTrue(ledger.IsInDebt);

            // TODO: Timing issue here.  ipfsOther could have sent the block
            // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();

            _ipfs.Options.Discovery = new();
            _ipfsOther.Options.Discovery = new();

            (await _ipfs.BitswapService).Protocols = originalProtocols;
            (await _ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
        }
    }

    [TestMethod]
    public async Task GetsBlock_OnConnect_Bitswap11()
    {
        var originalProtocols = (await _ipfs.BitswapService).Protocols;
        var otherOriginalProtocols = (await _ipfsOther.BitswapService).Protocols;

        (await _ipfs.BitswapService).Protocols = new IBitswapProtocol[]
        {
            new Bitswap11 { Bitswap = await _ipfs.BitswapService }
        };
        _ipfs.Options.Discovery.DisableMdns = true;
        _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfs.StartAsync();

        (await _ipfsOther.BitswapService).Protocols = new IBitswapProtocol[]
        {
            new Bitswap11 { Bitswap = await _ipfsOther.BitswapService }
        };
        _ipfsOther.Options.Discovery.DisableMdns = true;
        _ipfsOther.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfsOther.StartAsync();
        try
        {
            var data = Guid.NewGuid().ToByteArray();
            var cid = await _ipfsOther.Block.PutAsync(data);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var getTask = _ipfs.Block.GetAsync(cid, cts.Token);

            var remote = await _ipfsOther.LocalPeer;
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);
            var block = await getTask;

            Assert.IsFalse(getTask.IsCanceled, "task cancelled");
            Assert.IsFalse(getTask.IsFaulted, "task faulted");
            Assert.IsTrue(getTask.IsCompleted, "task not completed");
            Assert.AreEqual(cid, block.Id);
            CollectionAssert.AreEqual(data, block.DataBytes);

            var otherPeer = await _ipfsOther.LocalPeer;
            var ledger = await _ipfs.Bitswap.LedgerAsync(otherPeer, cts.Token);
            Assert.AreEqual(otherPeer, ledger.Peer);
            Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
            Assert.AreNotEqual(0UL, ledger.DataReceived);
            Assert.AreEqual(0UL, ledger.DataSent);
            Assert.IsTrue(ledger.IsInDebt);

            // TODO: Timing issue here.  ipfsOther could have sent the block
            // but not updated the stats yet.
#if false
                var localPeer = await ipfs.LocalPeer;
                ledger = await ipfsOther.Bitswap.LedgerAsync(localPeer);
                Assert.AreEqual(localPeer, ledger.Peer);
                Assert.AreNotEqual(0UL, ledger.BlocksExchanged);
                Assert.AreEqual(0UL, ledger.DataReceived);
                Assert.AreNotEqual(0UL, ledger.DataSent);
                Assert.IsFalse(ledger.IsInDebt);
#endif
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();

            _ipfs.Options.Discovery = new();
            _ipfsOther.Options.Discovery = new();

            (await _ipfs.BitswapService).Protocols = originalProtocols;
            (await _ipfsOther.BitswapService).Protocols = otherOriginalProtocols;
        }
    }

    [TestMethod]
    public async Task GetsBlock_OnRequest()
    {
        _ipfs.Options.Discovery.DisableMdns = true;
        _ipfs.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfs.StartAsync();

        _ipfsOther.Options.Discovery.DisableMdns = true;
        _ipfsOther.Options.Discovery.BootstrapPeers = Array.Empty<MultiAddress>();
        await _ipfsOther.StartAsync();
        try
        {
            var cts = new CancellationTokenSource(10000);
            var data = Guid.NewGuid().ToByteArray();
            var cid = await _ipfsOther.Block.PutAsync(data, cancel: cts.Token);

            var remote = await _ipfsOther.LocalPeer;
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First(), cts.Token);

            var block = await _ipfs.Block.GetAsync(cid, cts.Token);
            Assert.AreEqual(cid, block.Id);
            CollectionAssert.AreEqual(data, block.DataBytes);
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();
            _ipfs.Options.Discovery = new();
            _ipfsOther.Options.Discovery = new();
        }
    }

    [TestMethod]
    public async Task GetsBlock_Cidv1()
    {
        await _ipfs.StartAsync();
        await _ipfsOther.StartAsync();
        try
        {
            var data = Guid.NewGuid().ToByteArray();
            var cid = await _ipfsOther.Block.PutAsync(data, "raw", "sha2-512");

            var remote = await _ipfsOther.LocalPeer;
            await _ipfs.Swarm.ConnectAsync(remote.Addresses.First());

            var cts = new CancellationTokenSource(3000);
            var block = await _ipfs.Block.GetAsync(cid, cts.Token);
            Assert.AreEqual(cid, block.Id);
            CollectionAssert.AreEqual(data, block.DataBytes);
        }
        finally
        {
            await _ipfsOther.StopAsync();
            await _ipfs.StopAsync();
        }
    }

    [TestMethod]
    public async Task GetBlock_Timeout()
    {
        var block = new DagNode("BitswapApiTest unknown block"u8.ToArray());

        await _ipfs.StartAsync();
        try
        {
            var cts = new CancellationTokenSource(300);
            ExceptionAssert.Throws<TaskCanceledException>(() =>
            {
                var _ = _ipfs.Bitswap.GetAsync(block.Id, cts.Token).Result;
            });

            Assert.AreEqual(0, (await _ipfs.Bitswap.WantsAsync(cancel: cts.Token)).Count());
        }
        finally
        {
            await _ipfs.StopAsync();
        }
    }

    [TestMethod]
    public async Task PeerLedger()
    {
        await _ipfs.StartAsync();
        try
        {
            var peer = await _ipfsOther.LocalPeer;
            var ledger = await _ipfs.Bitswap.LedgerAsync(peer);
            Assert.IsNotNull(ledger);
        }
        finally
        {
            await _ipfs.StopAsync();
        }
    }
}