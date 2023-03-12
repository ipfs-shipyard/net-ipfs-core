using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ipfs.Engine.Tests.CoreApi;

[TestClass]
public class BlockRepositoryApiTest
{
    private readonly IpfsEngine _ipfs = TestFixture.Ipfs;

    [TestMethod]
    public void Exists()
    {
        Assert.IsNotNull(_ipfs.BlockRepository);
    }

    [TestMethod]
    public async Task Stats()
    {
        var stats = await _ipfs.BlockRepository.StatisticsAsync();
        var version = await _ipfs.BlockRepository.VersionAsync();
        Assert.AreEqual(stats.Version, version);
    }

    [TestMethod]
    public async Task GarbageCollection()
    {
        var pinned = await _ipfs.Block.PutAsync(new byte[256], pin: true);
        var unpinned = await _ipfs.Block.PutAsync(new byte[512], pin: false);
        Assert.AreNotEqual(pinned, unpinned);
        Assert.IsNotNull(await _ipfs.Block.StatAsync(pinned));
        Assert.IsNotNull(await _ipfs.Block.StatAsync(unpinned));

        await _ipfs.BlockRepository.RemoveGarbageAsync();
        Assert.IsNotNull(await _ipfs.Block.StatAsync(pinned));
        Assert.IsNull(await _ipfs.Block.StatAsync(unpinned));
    }

    [TestMethod]
    public async Task VersionFileMissing()
    {
        var versionPath = Path.Combine(_ipfs.Options.Repository.ExistingFolder(), "version");
        var versionBackupPath = versionPath + ".bak";

        try
        {
            if (File.Exists(versionPath))
            {
                File.Move(versionPath, versionBackupPath);
            }

            Assert.AreEqual("0", await _ipfs.BlockRepository.VersionAsync());
        }
        finally
        {
            if (File.Exists(versionBackupPath))
            {
                File.Move(versionBackupPath, versionPath);
            }
        }
    }
}