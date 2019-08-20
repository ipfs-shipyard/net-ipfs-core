using Ipfs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class MultiAddressBlackListTest
    {
        MultiAddress a = "/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        MultiAddress a1 = "/ip4/127.0.0.1/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        MultiAddress b = "/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
        MultiAddress c = "/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
        MultiAddress d = "/p2p/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";

        [TestMethod]
        public void Allowed()
        {
            var policy = new MultiAddressBlackList();
            policy.Add(a);
            policy.Add(b);
            Assert.IsFalse(policy.IsAllowed(a));
            Assert.IsFalse(policy.IsAllowed(a1));
            Assert.IsFalse(policy.IsAllowed(b));
            Assert.IsTrue(policy.IsAllowed(c));
            Assert.IsTrue(policy.IsAllowed(d));
        }

        [TestMethod]
        public void Allowed_Alias()
        {
            var policy = new MultiAddressBlackList();
            policy.Add(a);
            Assert.IsFalse(policy.IsAllowed(a));
            Assert.IsFalse(policy.IsAllowed(a1));
            Assert.IsFalse(policy.IsAllowed(b));
            Assert.IsTrue(policy.IsAllowed(c));
            Assert.IsTrue(policy.IsAllowed(d));
        }

        [TestMethod]
        public void Empty()
        {
            var policy = new MultiAddressBlackList();
            Assert.IsTrue( policy.IsAllowed(a));
        }
    }
}
