using System.Linq;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests.PubSub;

[TestClass]
public class TopicManagerTest
{
    Peer _a = new Peer { Id = "QmXK9VBxaXFuuT29AaPUTgW3jBWZ9JgLVZYdMYTHC6LLAH" };
    Peer _b = new Peer { Id = "QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ" };

    [TestMethod]
    public void Adding()
    {
        var topics = new TopicManager();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());

        topics.AddInterest("alpha", _a);
        Assert.AreEqual(_a, topics.GetPeers("alpha").First());

        topics.AddInterest("alpha", _b);
        var peers = topics.GetPeers("alpha").ToArray();
        CollectionAssert.Contains(peers, _a);
        CollectionAssert.Contains(peers, _b);
    }

    [TestMethod]
    public void Adding_Duplicate()
    {
        var topics = new TopicManager();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());

        topics.AddInterest("alpha", _a);
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());

        topics.AddInterest("alpha", _a);
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());

        topics.AddInterest("alpha", _b);
        Assert.AreEqual(2, topics.GetPeers("alpha").Count());
    }

    [TestMethod]
    public void Removing()
    {
        var topics = new TopicManager();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());

        topics.AddInterest("alpha", _a);
        topics.AddInterest("alpha", _b);
        Assert.AreEqual(2, topics.GetPeers("alpha").Count());

        topics.RemoveInterest("alpha", _a);
        Assert.AreEqual(_b, topics.GetPeers("alpha").First());
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());

        topics.RemoveInterest("alpha", _a);
        Assert.AreEqual(_b, topics.GetPeers("alpha").First());
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());

        topics.RemoveInterest("alpha", _b);
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());

        topics.RemoveInterest("beta", _b);
        Assert.AreEqual(0, topics.GetPeers("beta").Count());
    }

    [TestMethod]
    public void Clearing_Peers()
    {
        var topics = new TopicManager();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());
        Assert.AreEqual(0, topics.GetPeers("beta").Count());

        topics.AddInterest("alpha", _a);
        topics.AddInterest("beta", _a);
        topics.AddInterest("beta", _b);
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());
        Assert.AreEqual(2, topics.GetPeers("beta").Count());

        topics.Clear(_a);
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());
        Assert.AreEqual(1, topics.GetPeers("beta").Count());
    }


    [TestMethod]
    public void Clearing()
    {
        var topics = new TopicManager();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());
        Assert.AreEqual(0, topics.GetPeers("beta").Count());

        topics.AddInterest("alpha", _a);
        topics.AddInterest("beta", _b);
        Assert.AreEqual(1, topics.GetPeers("alpha").Count());
        Assert.AreEqual(1, topics.GetPeers("beta").Count());

        topics.Clear();
        Assert.AreEqual(0, topics.GetPeers("alpha").Count());
        Assert.AreEqual(0, topics.GetPeers("beta").Count());
    }

    [TestMethod]
    public void PeerTopics()
    {
        var tm = new TopicManager();
        tm.AddInterest("alpha", _a);
        CollectionAssert.AreEquivalent(new string[] { "alpha" }, tm.GetTopics(_a).ToArray());
        CollectionAssert.AreEquivalent(new string[0], tm.GetTopics(_b).ToArray());

        tm.AddInterest("beta", _a);
        CollectionAssert.AreEquivalent(new string[] { "alpha", "beta" }, tm.GetTopics(_a).ToArray());
        CollectionAssert.AreEquivalent(new string[0], tm.GetTopics(_b).ToArray());

        tm.AddInterest("beta", _b);
        CollectionAssert.AreEquivalent(new string[] { "alpha", "beta" }, tm.GetTopics(_a).ToArray());
        CollectionAssert.AreEquivalent(new string[] { "beta" }, tm.GetTopics(_b).ToArray());

    }
}