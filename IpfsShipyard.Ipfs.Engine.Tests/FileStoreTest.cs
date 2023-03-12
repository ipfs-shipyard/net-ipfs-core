using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests;

[TestClass]
public class FileStoreTest
{
    private readonly Entity _a = new() { Number = 1, Value = "a" };
    private readonly Entity _b = new() { Number = 2, Value = "b" };

    private static FileStore<int, Entity> Store
    {
        get
        {
            var folder = Path.Combine(TestFixture.Ipfs.Options.Repository.Folder, "test-filestore");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return new()
            {
                Folder = folder,
                NameToKey = name => name.ToString(),
                KeyToName = int.Parse
            };
        }
    }

    [TestMethod]
    public async Task PutAndGet()
    {
        var store = Store;

        await store.PutAsync(_a.Number, _a);
        await store.PutAsync(_b.Number, _b);

        var a1 = await store.GetAsync(_a.Number);
        Assert.AreEqual(_a.Number, a1.Number);
        Assert.AreEqual(_a.Value, a1.Value);

        var b1 = await store.GetAsync(_b.Number);
        Assert.AreEqual(_b.Number, b1.Number);
        Assert.AreEqual(_b.Value, b1.Value);
    }

    [TestMethod]
    public async Task TryGet()
    {
        var store = Store;
        await store.PutAsync(3, _a);
        var a1 = await store.GetAsync(3);
        Assert.AreEqual(_a.Number, a1.Number);
        Assert.AreEqual(_a.Value, a1.Value);

        var a3 = await store.TryGetAsync(42);
        Assert.IsNull(a3);
    }

    [TestMethod]
    public void Get_Unknown()
    {
        var store = Store;

        ExceptionAssert.Throws<KeyNotFoundException>(() =>
        {
            var _ = Store.GetAsync(42).Result;
        });
    }

    [TestMethod]
    public async Task Remove()
    {
        var store = Store;
        await store.PutAsync(4, _a);
        Assert.IsNotNull(await store.TryGetAsync(4));

        await store.RemoveAsync(4);
        Assert.IsNull(await store.TryGetAsync(4));
    }

    [TestMethod]
    public async Task Remove_Unknown()
    {
        var store = Store;
        await store.RemoveAsync(5);
    }

    [TestMethod]
    public async Task Length()
    {
        var store = Store;
        await store.PutAsync(6, _a);
        var length = await store.LengthAsync(6);
        Assert.IsTrue(length.HasValue);
        Assert.AreNotEqual(0, length.Value);
    }

    [TestMethod]
    public async Task Length_Unknown()
    {
        var store = Store;
        var length = await store.LengthAsync(7);
        Assert.IsFalse(length.HasValue);
    }

    [TestMethod]
    public async Task Values()
    {
        var store = Store;
        await store.PutAsync(8, new() { Value = "v0" });
        await store.PutAsync(9, new() { Value = "v1" });
        await store.PutAsync(10, new() { Value = "v0" });
        var values = Store.Values.Where(e => e.Value == "v0").ToArray();
        Assert.AreEqual(2, values.Length);
    }

    [TestMethod]
    public async Task Names()
    {
        var store = Store;
        await store.PutAsync(11, _a);
        await store.PutAsync(12, _a);
        await store.PutAsync(13, _a);
        var names = Store.Names.Where(n => n == 11 || n == 13).ToArray();
        Assert.AreEqual(2, names.Length);
    }

    [TestMethod]
    public async Task Atomic()
    {
        var store = Store;
        var nTasks = 100;
        var tasks = Enumerable
            .Range(1, nTasks)
            .Select(i => Task.Run(() => AtomicTask(store)))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task AtomicTask(FileStore<int, Entity> store)
    {
        await store.PutAsync(1, _a);
        await store.TryGetAsync(1);
        await store.RemoveAsync(1);
        var names = store.Names;
        var values = store.Values;
    }

    [TestMethod]
    public void PutWithException()
    {
        Task BadSerialize(Stream stream, int name, Entity value, CancellationToken cancel) => throw new("no serializer");
        var store = Store;
        store.Serialize = BadSerialize;

        ExceptionAssert.Throws<Exception>(() => store.PutAsync(_a.Number, _a).Wait());
        Assert.IsFalse(store.ExistsAsync(_a.Number).Result);
    }

    private class Entity
    {
        public int Number;
        public string Value;
    }
}