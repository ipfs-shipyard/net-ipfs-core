using System;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Engine.Migration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.Migration;

[TestClass]
public class MigrationManagerTest
{
    [TestMethod]
    public void HasMigrations()
    {
        var migrator = new MigrationManager(TestFixture.Ipfs);
        var migrations = migrator.Migrations;
        Assert.AreNotEqual(0, migrations.Count);
    }

    [TestMethod]
    public void MirgrateToUnknownVersion()
    {
        var migrator = new MigrationManager(TestFixture.Ipfs);
        ExceptionAssert.Throws<ArgumentOutOfRangeException>(() =>
        {
            migrator.MigrateToVersionAsync(int.MaxValue).Wait();
        });
    }

    [TestMethod]
    public async Task MigrateToLowestThenHighest()
    {
        using var ipfs = new TempNode();
        var migrator = new MigrationManager(ipfs);
        await migrator.MigrateToVersionAsync(0);
        Assert.AreEqual(0, migrator.CurrentVersion);

        await migrator.MigrateToVersionAsync(migrator.LatestVersion);
        Assert.AreEqual(migrator.LatestVersion, migrator.CurrentVersion);
    }
}