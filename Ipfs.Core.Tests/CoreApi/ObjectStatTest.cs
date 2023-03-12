﻿using IpfsShipyard.Ipfs.Core.CoreApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Core.Tests.CoreApi
{
    [TestClass]
    public class ObjectStatTest
    {
        [TestMethod]
        public void Properties()
        {
            var stat = new ObjectStat
            {
                BlockSize = 1,
                CumulativeSize = 2,
                DataSize = 3,
                LinkCount = 4,
                LinkSize = 5
            };
            Assert.AreEqual(1, stat.BlockSize);
            Assert.AreEqual(2, stat.CumulativeSize);
            Assert.AreEqual(3, stat.DataSize);
            Assert.AreEqual(4, stat.LinkCount);
            Assert.AreEqual(5, stat.LinkSize);
        }

    }
}
