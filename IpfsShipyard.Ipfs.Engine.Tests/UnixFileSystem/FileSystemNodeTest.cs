using IpfsShipyard.Ipfs.Engine.UnixFileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.UnixFileSystem;

[TestClass]
public class FileSystemNodeTest
{
    [TestMethod]
    public void ToLink()
    {
        var node = new FileSystemNode
        {
            Name = "bar",
            Id = "Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD",
            IsDirectory = true,
            Size = 10,
            DagSize = 16
        };
        var link = node.ToLink("foo");
        Assert.AreEqual(node.Id, link.Id);
        Assert.AreEqual(node.DagSize, link.Size);
        Assert.AreEqual("foo", link.Name);

        link = node.ToLink();
        Assert.AreEqual(node.Id, link.Id);
        Assert.AreEqual(node.DagSize, link.Size);
        Assert.AreEqual("bar", link.Name);
    }
}