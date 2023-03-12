using System;
using System.Linq;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.Routing;

[TestClass]
public class ContentRouterTest
{
    readonly Peer _self = new Peer
    {
        AgentVersion = "self",
        Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
        PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
    };

    readonly Peer _other = new Peer
    {
        AgentVersion = "other",
        Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
        Addresses = new MultiAddress[]
        {
            new MultiAddress("/ip4/127.0.0.1/tcp/4001")
        }
    };

    readonly Cid _cid1 = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";

    [TestMethod]
    public void Add()
    {
        using (var router = new ContentRouter())
        {
            router.Add(_cid1, _self.Id);

            var providers = router.Get(_cid1);
            Assert.AreEqual(1, providers.Count());
            Assert.AreEqual(_self.Id, providers.First());
        }
    }

    [TestMethod]
    public void Add_Duplicate()
    {
        using (var router = new ContentRouter())
        {
            router.Add(_cid1, _self.Id);
            router.Add(_cid1, _self.Id);

            var providers = router.Get(_cid1);
            Assert.AreEqual(1, providers.Count());
            Assert.AreEqual(_self.Id, providers.First());
        }
    }

    [TestMethod]
    public void Add_MultipleProviders()
    {
        using (var router = new ContentRouter())
        {
            router.Add(_cid1, _self.Id);
            router.Add(_cid1, _other.Id);

            var providers = router.Get(_cid1).ToArray();
            Assert.AreEqual(2, providers.Length);
            CollectionAssert.Contains(providers, _self.Id);
            CollectionAssert.Contains(providers, _other.Id);
        }
    }

    [TestMethod]
    public void Get_NonexistentCid()
    {
        using (var router = new ContentRouter())
        {
            var providers = router.Get(_cid1);
            Assert.AreEqual(0, providers.Count());
        }
    }

    [TestMethod]
    public void Get_Expired()
    {
        using (var router = new ContentRouter())
        {
            router.Add(_cid1, _self.Id, DateTime.MinValue);

            var providers = router.Get(_cid1);
            Assert.AreEqual(0, providers.Count());
        }
    }

    [TestMethod]
    public void Get_NotExpired()
    {
        using (var router = new ContentRouter())
        {
            router.Add(_cid1, _self.Id, DateTime.MinValue);
            var providers = router.Get(_cid1);
            Assert.AreEqual(0, providers.Count());

            router.Add(_cid1, _self.Id, DateTime.MaxValue - router.ProviderTtl);
            providers = router.Get(_cid1);
            Assert.AreEqual(1, providers.Count());
        }
    }
}