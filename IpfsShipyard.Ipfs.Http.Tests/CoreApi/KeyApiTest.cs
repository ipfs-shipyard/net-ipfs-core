using System;
using System.Linq;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi;

[TestClass]
public class KeyApiTest
{
    [TestMethod]
    public void Api_Exists()
    {
        var ipfs = TestFixture.Ipfs;
        Assert.IsNotNull(ipfs.Key);
    }

    [TestMethod]
    public async Task Self_Key_Exists()
    {
        var ipfs = TestFixture.Ipfs;
        var keys = await ipfs.Key.ListAsync();
        var self = Enumerable.Single<IKey>(keys, k => k.Name == "self");
        var me = await ipfs.IdAsync();
        Assert.AreEqual("self", self.Name);
        Assert.AreEqual(me.Id, self.Id);
    }

    [TestMethod]
    public async Task Create_RSA_Key()
    {
        var name = "net-api-test-create";
        var ipfs = TestFixture.Ipfs;
        var key = await ipfs.Key.CreateAsync(name, "rsa", 1024);
        try
        {
            Assert.IsNotNull(key);
            Assert.IsNotNull(key.Id);
            Assert.AreEqual<string>(name, key.Name);

            var keys = await ipfs.Key.ListAsync();
            var clone = Enumerable.Single<IKey>(keys, k => k.Name == name);
            Assert.AreEqual<string>(key.Name, clone.Name);
            Assert.AreEqual<MultiHash>(key.Id, clone.Id);
        }
        finally
        {
            await ipfs.Key.RemoveAsync(name);
        }
    }

    [TestMethod]
    public async Task Remove_Key()
    {
        var name = "net-api-test-remove";
        var ipfs = TestFixture.Ipfs;
        var key = await ipfs.Key.CreateAsync(name, "rsa", 1024);
        var keys = await ipfs.Key.ListAsync();
        var clone = Enumerable.Single<IKey>(keys, k => k.Name == name);
        Assert.IsNotNull(clone);

        var removed = await ipfs.Key.RemoveAsync(name);
        Assert.IsNotNull(removed);
        Assert.AreEqual<string>(key.Name, removed.Name);
        Assert.AreEqual<MultiHash>(key.Id, removed.Id);

        keys = await ipfs.Key.ListAsync();
        Assert.IsFalse(Enumerable.Any<IKey>(keys, k => k.Name == name));
    }

    [TestMethod]
    public async Task Rename_Key()
    {
        var oname = "net-api-test-rename1";
        var rname = "net-api-test-rename2";
        var ipfs = TestFixture.Ipfs;
        var okey = await ipfs.Key.CreateAsync(oname, "rsa", 1024);
        try
        {
            Assert.AreEqual<string>(oname, okey.Name);

            var rkey = await ipfs.Key.RenameAsync(oname, rname);
            Assert.AreEqual<MultiHash>(okey.Id, rkey.Id);
            Assert.AreEqual<string>(rname, rkey.Name);

            var keys = await ipfs.Key.ListAsync();
            Assert.IsTrue(Enumerable.Any<IKey>(keys, k => k.Name == rname));
            Assert.IsFalse(Enumerable.Any<IKey>(keys, k => k.Name == oname));
        }
        finally
        {
            try
            {
                await ipfs.Key.RemoveAsync(oname);
            }
            catch (Exception) { }
            try
            {
                await ipfs.Key.RemoveAsync(rname);
            }
            catch (Exception) { }
        }
    }

}