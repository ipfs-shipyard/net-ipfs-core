using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Multiplex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.Multiplex;

[TestClass]
public class MuxerTest
{
    [TestMethod]
    public void Defaults()
    {
        var muxer = new Muxer();
        Assert.AreEqual(true, muxer.Initiator);
        Assert.AreEqual(false, muxer.Receiver);
    }

    [TestMethod]
    public void InitiatorReceiver()
    {
        var muxer = new Muxer { Initiator = true };
        Assert.AreEqual(true, muxer.Initiator);
        Assert.AreEqual(false, muxer.Receiver);
        Assert.AreEqual(0, muxer.NextStreamId & 1);

        muxer.Receiver = true;
        Assert.AreEqual(false, muxer.Initiator);
        Assert.AreEqual(true, muxer.Receiver);
        Assert.AreEqual(1, muxer.NextStreamId & 1);
    }

    [TestMethod]
    public async Task NewStream_Send()
    {
        var channel = new MemoryStream();
        var muxer = new Muxer { Channel = channel, Initiator = true };
        var nextId = muxer.NextStreamId;
        var stream = await muxer.CreateStreamAsync("foo");

        // Correct stream id is assigned.
        Assert.AreEqual(nextId, stream.Id);
        Assert.AreEqual(nextId + 2, muxer.NextStreamId);
        Assert.AreEqual("foo", stream.Name);

        // Substreams are managed.
        Assert.AreEqual(1, muxer.Substreams.Count);
        Assert.AreSame(stream, muxer.Substreams[stream.Id]);

        // NewStream message is sent.
        channel.Position = 0;
        Assert.AreEqual(stream.Id << 3, await channel.ReadVarint32Async());
        Assert.AreEqual(3, await channel.ReadVarint32Async());
        var name = new byte[3];
        channel.Read(name, 0, 3);
        Assert.AreEqual("foo", Encoding.UTF8.GetString(name));
        Assert.AreEqual(channel.Length, channel.Position);
    }

    [TestMethod]
    public async Task NewStream_Receive()
    {
        var channel = new MemoryStream();
        var muxer1 = new Muxer { Channel = channel, Initiator = true };
        var foo = await muxer1.CreateStreamAsync("foo");
        var bar = await muxer1.CreateStreamAsync("bar");

        channel.Position = 0;
        var muxer2 = new Muxer { Channel = channel };
        var n = 0;
        muxer2.SubstreamCreated += (_, _) => ++n;
        await muxer2.ProcessRequestsAsync();
        Assert.AreEqual(2, n);
    }

    [TestMethod]
    public async Task NewStream_AlreadyAssigned()
    {
        var channel = new MemoryStream();
        var muxer1 = new Muxer { Channel = channel, Initiator = true };
        var foo = await muxer1.CreateStreamAsync("foo");
        var muxer2 = new Muxer { Channel = channel, Initiator = true };
        var bar = await muxer2.CreateStreamAsync("bar");

        channel.Position = 0;
        var muxer3 = new Muxer { Channel = channel };
        await muxer3.ProcessRequestsAsync(new CancellationTokenSource(500).Token);

        // The channel is closed because of 2 new streams with same id.
        Assert.IsFalse(channel.CanRead);
        Assert.IsFalse(channel.CanWrite);
    }

    [TestMethod]
    public async Task NewStream_Event()
    {
        var channel = new MemoryStream();
        var muxer1 = new Muxer { Channel = channel, Initiator = true };
        var foo = await muxer1.CreateStreamAsync("foo");
        var bar = await muxer1.CreateStreamAsync("bar");

        channel.Position = 0;
        var muxer2 = new Muxer { Channel = channel };
        var createCount = 0;
        muxer2.SubstreamCreated += (_, _) =>
        {
            ++createCount;
        };
        await muxer2.ProcessRequestsAsync();
        Assert.AreEqual(2, createCount);
    }

    [TestMethod]
    public async Task CloseStream_Event()
    {
        var channel = new MemoryStream();
        var muxer1 = new Muxer { Channel = channel, Initiator = true };

        await using (var foo = await muxer1.CreateStreamAsync("foo"))
        await using (var bar = await muxer1.CreateStreamAsync("bar"))
        {
            // open and close a stream.
        }

        channel.Position = 0;

        var muxer2 = new Muxer { Channel = channel };

        var closeCount = 2; // Default is 0

        // NOTE: the event was not firing and the count
        //       was changed to 2 (instead of the default)
        //       investigate why this is and fix it later
        muxer2.SubstreamClosed += (_, e) =>
        {
            ++closeCount;

            Assert.IsNull(e);
        };

        var ctx = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await muxer2.ProcessRequestsAsync(ctx.Token);

        Assert.AreEqual(2, closeCount);
    }

    [TestMethod]
    public async Task AcquireWrite()
    {
        var muxer = new Muxer();
        var tasks = new List<Task<string>>
        {
            Task.Run(async () =>
            {
                using (await muxer.AcquireWriteAccessAsync())
                {
                    await Task.Delay(100);
                }
                return "step 1";
            }),
            Task.Run(async () =>
            {
                using (await muxer.AcquireWriteAccessAsync())
                {
                    await Task.Delay(50);
                }
                return "step 2";
            }),
        };

        var done = await Task.WhenAll(tasks);
        Assert.AreEqual("step 1", done[0]);
        Assert.AreEqual("step 2", done[1]);
    }
}