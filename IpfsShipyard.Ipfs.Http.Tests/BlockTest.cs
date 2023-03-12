using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests;

[TestClass]
public class BlockTest
{
    private byte[] _someBytes = new byte[] { 1, 2, 3 };

    [TestMethod]
    public void DataBytes()
    {
        var block = new Block
        {
            DataBytes = _someBytes
        };
        CollectionAssert.AreEqual(_someBytes, block.DataBytes);
    }

    [TestMethod]
    public void DataStream()
    {
        var block = new Block
        {
            DataBytes = _someBytes
        };
        var stream = block.DataStream;
        Assert.AreEqual(1, stream.ReadByte());
        Assert.AreEqual(2, stream.ReadByte());
        Assert.AreEqual(3, stream.ReadByte());
        Assert.AreEqual(-1, stream.ReadByte(), "at eof");
    }

}