﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{
    [TestClass]
    public class DhtApiTest
    {

        [TestMethod]
        public async Task Local_Info()
        {
            var ipfs = TestFixture.Ipfs;
            var locaId = (await ipfs.LocalPeer).Id;
            var peer = await ipfs.Dht.FindPeerAsync(locaId);

            Assert.IsInstanceOfType(peer, typeof(Peer));
            Assert.AreEqual(locaId, peer.Id);
            Assert.IsNotNull(peer.Addresses);
            Assert.IsTrue(peer.IsValid());
        }

        [TestMethod]
        [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74#issuecomment-500668261")]
        public async Task Mars_Info()
        {
            var marsId = "QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var mars = await ipfs.Dht.FindPeerAsync(marsId, cts.Token);
                Assert.AreEqual(marsId, mars.Id);
                Assert.IsNotNull(mars.Addresses);
                Assert.IsTrue(mars.IsValid());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
        public async Task FindProvider()
        {
            var folder = "QmS4ustL54uo8FzR9455qaxZwuMiUhyvMcX9Ba8nUH4uVv";
            var ipfs = TestFixture.Ipfs;
            await ipfs.StartAsync();
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var providers = await ipfs.Dht.FindProvidersAsync(folder, 1, null, cts.Token);
                Assert.AreEqual(1, providers.Count());
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

    }
}

