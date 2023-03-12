using System;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Core.Tests.CoreApi
{
    [TestClass]
    public class PingResultTest
    {
        [TestMethod]
        public void Properties()
        {
            var time = TimeSpan.FromSeconds(3);
            var r = new PingResult
            {
                Success = true,
                Text = "ping",
                Time = time
            };
            Assert.AreEqual(true, r.Success);
            Assert.AreEqual("ping", r.Text);
            Assert.AreEqual(time, r.Time);
        }

    }
}
