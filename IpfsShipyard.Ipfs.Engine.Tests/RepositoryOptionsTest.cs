using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests;

[TestClass]
public class RepositoryOptionsTest
{
    [TestMethod]
    public void Defaults()
    {
        var options = new RepositoryOptions();
        Assert.IsNotNull(options.Folder);
    }


    [TestMethod]
    public void Environment_Home()
    {
        var names = new[] { "IPFS_PATH", "HOME", "HOMEPATH" };
        var values = names.Select(Environment.GetEnvironmentVariable);
        var sep = Path.DirectorySeparatorChar;
        try
        {
            foreach (var name in names)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            Environment.SetEnvironmentVariable("HOME", $"{sep}home1");
            var options = new RepositoryOptions();
            Assert.AreEqual($"{sep}home1{sep}.csipfs", options.Folder);

            Environment.SetEnvironmentVariable("HOME", $"{sep}home2{sep}");
            options = new();
            Assert.AreEqual($"{sep}home2{sep}.csipfs", options.Folder);
        }
        finally
        {
            var pairs = names.Zip(values, (name, value) => new { name, value });
            foreach (var pair in pairs)
            {
                Environment.SetEnvironmentVariable(pair.name, pair.value);
            }
        }
    }

    [TestMethod]
    public void Environment_HomePath()
    {
        var names = new[] { "IPFS_PATH", "HOME", "HOMEPATH" };
        var values = names.Select(Environment.GetEnvironmentVariable);
        var sep = Path.DirectorySeparatorChar;
        try
        {
            foreach (var name in names)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            Environment.SetEnvironmentVariable("HOMEPATH", $"{sep}home1");
            var options = new RepositoryOptions();
            Assert.AreEqual($"{sep}home1{sep}.csipfs", options.Folder);

            Environment.SetEnvironmentVariable("HOMEPATH", $"{sep}home2{sep}");
            options = new();
            Assert.AreEqual($"{sep}home2{sep}.csipfs", options.Folder);
        }
        finally
        {
            var pairs = names.Zip(values, (name, value) => new { name, value });
            foreach (var pair in pairs)
            {
                Environment.SetEnvironmentVariable(pair.name, pair.value);
            }
        }
    }

    [TestMethod]
    public void Environment_IpfsPath()
    {
        var names = new[] { "IPFS_PATH", "HOME", "HOMEPATH" };
        var values = names.Select(Environment.GetEnvironmentVariable);
        var sep = Path.DirectorySeparatorChar;
        try
        {
            foreach (var name in names)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            Environment.SetEnvironmentVariable("IPFS_PATH", $"{sep}x1");
            var options = new RepositoryOptions();
            Assert.AreEqual($"{sep}x1", options.Folder);

            Environment.SetEnvironmentVariable("IPFS_PATH", $"{sep}x2{sep}");
            options = new();
            Assert.AreEqual($"{sep}x2{sep}", options.Folder);
        }
        finally
        {
            var pairs = names.Zip(values, (name, value) => new { name, value });
            foreach (var pair in pairs)
            {
                Environment.SetEnvironmentVariable(pair.name, pair.value);
            }
        }
    }
}