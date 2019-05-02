﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PeerTalk.Cryptography;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Engine
{

    [TestClass]
    public class SwarmApiTest
    {
        IpfsEngine ipfs = TestFixture.Ipfs;
        readonly MultiAddress somewhere = "/ip4/127.0.0.1";

        [TestMethod]
        public async Task Filter_Add_Remove()
        {
            var addr = await ipfs.Swarm.AddAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            var addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsTrue(addrs.Any(a => a == somewhere));

            addr = await ipfs.Swarm.RemoveAddressFilterAsync(somewhere);
            Assert.IsNotNull(addr);
            Assert.AreEqual(somewhere, addr);
            addrs = await ipfs.Swarm.ListAddressFiltersAsync();
            Assert.IsFalse(addrs.Any(a => a == somewhere));
        }

        [TestMethod]
        [Ignore("https://github.com/richardschneider/net-ipfs-engine/issues/74")]
        public async Task Connect_Disconnect_Mars()
        {
            var mars = "/dns/mars.i.ipfs.io/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ";
            await ipfs.StartAsync();
            try
            {
                await ipfs.Swarm.ConnectAsync(mars);
                var peers = await ipfs.Swarm.PeersAsync();
                Assert.IsTrue(peers.Any(p => p.Id == "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"));
                await ipfs.Swarm.DisconnectAsync(mars);
            }
            finally
            {
                await ipfs.StopAsync();
            }
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task JsIPFS_Connect()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmXFX2P5ammdmXQgfqGkfswtEVFsZUJ5KeHRXQYCTdiTAb";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4002/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task GoIPFS_Connect()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmdoxrwszT6b9srLXHYBPFVRXmZSFAosWLXoQS9TEEAaix";
            var remoteAddress = $"/ip4/127.0.0.1/tcp/4001/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

        [TestMethod]
        [Ignore("TODO: Move to interop tests")]
        public async Task GoIPFS_Connect_v0_4_17()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var remoteId = "QmSoLer265NRgSp2LA3dPaeykiS1J6DifTC88f5uVQKNAd";
            var remoteAddress = $"/ip4/178.62.158.247/tcp/4001/ipfs/{remoteId}";

            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
            await ipfs.Swarm.ConnectAsync(remoteAddress, cts.Token);
            Assert.AreEqual(1, (await ipfs.Swarm.PeersAsync()).Count());

            await ipfs.Swarm.DisconnectAsync(remoteAddress);
            Assert.AreEqual(0, (await ipfs.Swarm.PeersAsync()).Count());
        }

        [TestMethod]
        public async Task PrivateNetwork_WithOptionsKey()
        {
            using (var ipfs = CreateNode())
            {
                try
                {
                    ipfs.Options.Swarm.PrivateNetworkKey = new PreSharedKey().Generate();
                    var swarm = await ipfs.SwarmService;
                    Assert.IsNotNull(swarm.NetworkProtector);
                }
                finally
                {
                    if (Directory.Exists(ipfs.Options.Repository.Folder))
                    {
                        Directory.Delete(ipfs.Options.Repository.Folder, true);
                    }
                }
            }
        }

        [TestMethod]
        public async Task PrivateNetwork_WithSwarmKeyFile()
        {
            using (var ipfs = CreateNode())
            {
                try
                {
                    var key = new PreSharedKey().Generate();
                    var path = Path.Combine(ipfs.Options.Repository.ExistingFolder(), "swarm.key");
                    using (var x = File.CreateText(path))
                    {
                        key.Export(x);
                    }

                    var swarm = await ipfs.SwarmService;
                    Assert.IsNotNull(swarm.NetworkProtector);
                }
                finally
                {
                    if (Directory.Exists(ipfs.Options.Repository.Folder))
                    {
                        Directory.Delete(ipfs.Options.Repository.Folder, true);
                    }
                }
            }
        }

        static int nodeNumber = 0;
        IpfsEngine CreateNode()
        {
            const string passphrase = "this is not a secure pass phrase";
            var ipfs = new IpfsEngine(passphrase.ToCharArray());
            ipfs.Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"swarm-{nodeNumber++}");
            ipfs.Options.KeyChain.DefaultKeySize = 512;
            ipfs.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { "/ip4/0.0.0.0/tcp/4007" })
            ).Wait();

            return ipfs;
        }

    }
}
