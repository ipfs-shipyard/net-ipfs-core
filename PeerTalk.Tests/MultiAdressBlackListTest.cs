using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.PeerTalk.Tests;

[TestClass]
public class MultiAddressBlackListTest
{
    MultiAddress a = "/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
    MultiAddress a1 = "/ip4/127.0.0.1/ipfs/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
    MultiAddress b = "/p2p/QmSoLMeWqB7YGVLJN3pNLQpmmEk35v6wYtsMGLzSr5QBU3";
    MultiAddress c = "/ipfs/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";
    MultiAddress d = "/p2p/QmSoLV4Bbm51jM9C4gDYZQ9Cy3U6aXMJDAbzgu2fzaDs64";

    [TestMethod]
    public void Allowed()
    {
        var policy = new MultiAddressBlackList();
        policy.Add(a);
        policy.Add(b);
        Assert.IsFalse(policy.IsAllowed(a));
        Assert.IsFalse(policy.IsAllowed(a1));
        Assert.IsFalse(policy.IsAllowed(b));
        Assert.IsTrue(policy.IsAllowed(c));
        Assert.IsTrue(policy.IsAllowed(d));
    }

    [TestMethod]
    public void Allowed_Alias()
    {
        var policy = new MultiAddressBlackList();
        policy.Add(a);
        Assert.IsFalse(policy.IsAllowed(a));
        Assert.IsFalse(policy.IsAllowed(a1));
        Assert.IsFalse(policy.IsAllowed(b));
        Assert.IsTrue(policy.IsAllowed(c));
        Assert.IsTrue(policy.IsAllowed(d));
    }

    [TestMethod]
    public void Empty()
    {
        var policy = new MultiAddressBlackList();
        Assert.IsTrue( policy.IsAllowed(a));
    }

    [TestMethod]
    public void Collection()
    {
        MultiAddress a = "/ip4/127.0.0.1";
        MultiAddress b = "/ip4/127.0.0.2";

        var policy = new MultiAddressBlackList();
        Assert.IsFalse(policy.IsReadOnly);
        Assert.AreEqual(0, policy.Count);
        Assert.IsFalse(policy.Contains(a));
        Assert.IsFalse(policy.Contains(b));

        policy.Add(a);
        Assert.AreEqual(1, policy.Count);
        Assert.IsTrue(policy.Contains(a));
        Assert.IsFalse(policy.Contains(b));

        policy.Add(a);
        Assert.AreEqual(1, policy.Count);
        Assert.IsTrue(policy.Contains(a));
        Assert.IsFalse(policy.Contains(b));

        policy.Add(b);
        Assert.AreEqual(2, policy.Count);
        Assert.IsTrue(policy.Contains(a));
        Assert.IsTrue(policy.Contains(b));

        policy.Remove(b);
        Assert.AreEqual(1, policy.Count);
        Assert.IsTrue(policy.Contains(a));
        Assert.IsFalse(policy.Contains(b));

        var array = new MultiAddress[1];
        policy.CopyTo(array, 0);
        Assert.AreSame(a, array[0]);

        foreach (var filter in policy)
        {
            Assert.AreSame(a, filter);
        }

        policy.Clear();
        Assert.AreEqual(0, policy.Count);
        Assert.IsFalse(policy.Contains(a));
        Assert.IsFalse(policy.Contains(b));
    }
}