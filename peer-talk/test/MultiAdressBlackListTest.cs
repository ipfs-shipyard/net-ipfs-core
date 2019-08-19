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
        public async Task Allowed()
        {
            var policy = new MultiAddressBlackList();
            policy.Add(a);
            policy.Add(b);
            Assert.IsFalse(await policy.IsAllowedAsync(a));
            Assert.IsFalse(await policy.IsAllowedAsync(a1));
            Assert.IsFalse(await policy.IsAllowedAsync(b));
            Assert.IsTrue(await policy.IsAllowedAsync(c));
            Assert.IsTrue(await policy.IsAllowedAsync(d));
        }

        [TestMethod]
        public async Task Allowed_Alias()
        {
            var policy = new MultiAddressBlackList();
            policy.Add(a);
            Assert.IsFalse(await policy.IsAllowedAsync(a));
            Assert.IsFalse(await policy.IsAllowedAsync(a1));
            Assert.IsFalse(await policy.IsAllowedAsync(b));
            Assert.IsTrue(await policy.IsAllowedAsync(c));
            Assert.IsTrue(await policy.IsAllowedAsync(d));
        }

        [TestMethod]
        public async Task NotAllowed()
        {
            var policy = new MultiAddressBlackList();
            policy.Add(a);
            policy.Add(b);
            Assert.IsTrue(await policy.IsNotAllowedAsync(a));
            Assert.IsTrue(await policy.IsNotAllowedAsync(a1));
            Assert.IsTrue(await policy.IsNotAllowedAsync(b));
            Assert.IsFalse(await policy.IsNotAllowedAsync(c));
            Assert.IsFalse(await policy.IsNotAllowedAsync(d));
        }

        [TestMethod]
        public async Task Empty()
        {
            var policy = new MultiAddressBlackList();
            Assert.IsTrue(await policy.IsAllowedAsync(a));
            Assert.IsFalse(await policy.IsNotAllowedAsync(a));
        }
    }
}
