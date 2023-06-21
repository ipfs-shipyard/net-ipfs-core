using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs
{
    [TestClass]
    public class NamedContentTest
    {
        [TestMethod]
        public void Properties()
        {
            var nc = new NamedContent
            {
                ContentPath = "/ipfs/...",
                NamePath = "/ipns/..."
            };
            Assert.AreEqual("/ipfs/...", nc.ContentPath);
            Assert.AreEqual("/ipns/...", nc.NamePath);
        }
    }
}
