using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ipfs
{
    [TestClass]
    public class DagCidTest
    {
        [TestMethod]
        public void Value_ValidCid_SetsSuccessfully()
        {
            // Arrange
            Cid validCid = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";

            // Act & Assert
            var dagCid = new DagCid { Value = validCid };
            Assert.AreEqual(validCid, dagCid.Value);
        }

        [TestMethod]
        public void Value_LibP2pKeyCid_ThrowsArgumentException()
        {
            // Arrange - using real IPNS key CID that should have libp2p-key content type
            Cid libp2pKeyCid = "k51qzi5uqu5dlvj2baxnqndepeb86cbk3ng7n3i46uzyxzyqj2xjonzllnv0v8";
            
            // Verify this CID actually has libp2p-key content type
            Assert.AreEqual("libp2p-key", libp2pKeyCid.ContentType);

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => 
                new DagCid { Value = libp2pKeyCid });
            
            Assert.IsTrue(exception.Message.Contains("Cannot store CID-encoded libp2p key as DagCid link"));
            Assert.IsTrue(exception.Message.Contains("IPLD links must be immutable"));
            Assert.AreEqual("value", exception.ParamName);
        }

        [TestMethod]
        public void Value_LibP2pKeyCid_SetAfterConstruction_ThrowsArgumentException()
        {
            // Arrange
            Cid validCid = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";
            // Using another real IPNS key CID
            Cid libp2pKeyCid = "k51qzi5uqu5dlvj2baxnqndepeb86cbk3ng7n3i46uzyxzyqj2xjonzllnv0v8";

            var dagCid = new DagCid { Value = validCid };

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => 
                dagCid.Value = libp2pKeyCid);
            
            Assert.IsTrue(exception.Message.Contains("Cannot store CID-encoded libp2p key as DagCid link"));
            Assert.AreEqual("value", exception.ParamName);
        }

        [TestMethod]
        public void ExplicitCast_ValidCid_CastsSuccessfully()
        {
            // Arrange
            Cid validCid = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";

            // Act
            var dagCid = (DagCid)validCid;

            // Assert
            Assert.AreEqual(validCid, dagCid.Value);
        }

        [TestMethod]
        public void ExplicitCast_LibP2pKeyCid_ThrowsArgumentException()
        {
            // Arrange - using real IPNS key CID that should have libp2p-key content type
            Cid libp2pKeyCid = "k51qzi5uqu5dlvj2baxnqndepeb86cbk3ng7n3i46uzyxzyqj2xjonzllnv0v8";

            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() => 
                (DagCid)libp2pKeyCid);
            
            Assert.IsTrue(exception.Message.Contains("Cannot cast CID-encoded libp2p key to DagCid"));
            Assert.IsTrue(exception.Message.Contains("IPLD links must be immutable"));
            Assert.AreEqual("cid", exception.ParamName);
        }

        [TestMethod]
        public void ImplicitCast_DagCidToCid_WorksCorrectly()
        {
            // Arrange
            Cid originalCid = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";
            var dagCid = new DagCid { Value = originalCid };

            // Act
            Cid convertedCid = dagCid;

            // Assert
            Assert.AreEqual(originalCid, convertedCid);
        }

        [TestMethod]
        public void ToString_ReturnsValueToString()
        {
            // Arrange
            Cid cid = "QmXg9Pp2ytZ14xgmQjYEiHjVjMFXzCVVEcRTWJBmLgR39V";
            var dagCid = new DagCid { Value = cid };

            // Act
            var result = dagCid.ToString();

            // Assert
            Assert.AreEqual(cid.ToString(), result);
        }
    }
}
