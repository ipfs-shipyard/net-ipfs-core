using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk.Routing
{
    
    [TestClass]
    public class ContentRouterTest
    {
        Peer self = new Peer
        {
            AgentVersion = "self",
            Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH",
            PublicKey = "CAASXjBcMA0GCSqGSIb3DQEBAQUAA0sAMEgCQQCC5r4nQBtnd9qgjnG8fBN5+gnqIeWEIcUFUdCG4su/vrbQ1py8XGKNUBuDjkyTv25Gd3hlrtNJV3eOKZVSL8ePAgMBAAE="
        };

        Peer other = new Peer
        {
            AgentVersion = "other",
            Id = "QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCM1h",
            Addresses = new MultiAddress[]
            {
                new MultiAddress("/ip4/127.0.0.1/tcp/4001")
            }
        };

        Cid cid1 = "zBunRGrmCGokA1oMESGGTfrtcMFsVA8aEtcNzM54akPWXF97uXCqTjF3GZ9v8YzxHrG66J8QhtPFWwZebRZ2zeUEELu67";

        [TestMethod]
        public void Add()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);

                var providers = router.Get(cid1);
                Assert.AreEqual(1, providers.Count());
                Assert.AreEqual(self.Id, providers.First());
            }
        }

        [TestMethod]
        public void Add_Duplicate()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);
                router.Add(cid1, self.Id);

                var providers = router.Get(cid1);
                Assert.AreEqual(1, providers.Count());
                Assert.AreEqual(self.Id, providers.First());
            }
        }

        [TestMethod]
        public void Add_MultipleProviders()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id);
                router.Add(cid1, other.Id);

                var providers = router.Get(cid1).ToArray();
                Assert.AreEqual(2, providers.Length);
                CollectionAssert.Contains(providers, self.Id);
                CollectionAssert.Contains(providers, other.Id);
            }
        }

        [TestMethod]
        public void Get_NonexistentCid()
        {
            using (var router = new ContentRouter())
            {
                var providers = router.Get(cid1);
                Assert.AreEqual(0, providers.Count());
            }
        }

        [TestMethod]
        public void Get_Expired()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id, DateTime.MinValue);

                var providers = router.Get(cid1);
                Assert.AreEqual(0, providers.Count());
            }
        }

        [TestMethod]
        public void Get_NotExpired()
        {
            using (var router = new ContentRouter())
            {
                router.Add(cid1, self.Id, DateTime.MinValue);
                var providers = router.Get(cid1);
                Assert.AreEqual(0, providers.Count());

                router.Add(cid1, self.Id, DateTime.MaxValue - router.ProviderTTL);
                providers = router.Get(cid1);
                Assert.AreEqual(1, providers.Count());
            }
        }
    }
}
