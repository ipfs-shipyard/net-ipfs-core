using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PeerTalk
{
    [TestClass]
    public class PolicyTest
    {
        [TestMethod]
        public void Always()
        {
            var policy = new PolicyAlways<string>();
            Assert.IsTrue(policy.IsAllowed("foo"));
        }

        [TestMethod]
        public void Never()
        {
            var policy = new PolicyNever<string>();
            Assert.IsFalse(policy.IsAllowed("foo"));
        }
    }
}
