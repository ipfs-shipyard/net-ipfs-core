using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PeerTalk.Tests
{
    [TestClass]
    public class BlackListTest
    {
        [TestMethod]
        public void Allowed()
        {
            var policy = new BlackList<string>();
            policy.Add("c");
            policy.Add("d");
            Assert.IsTrue(policy.IsAllowed("a"));
            Assert.IsTrue(policy.IsAllowed("b"));
            Assert.IsFalse(policy.IsAllowed("c"));
            Assert.IsFalse(policy.IsAllowed("d"));
        }

        [TestMethod]
        public void Empty()
        {
            var policy = new BlackList<string>();
            Assert.IsTrue(policy.IsAllowed("a"));
        }
    }
}
