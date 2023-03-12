using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;

namespace IpfsShipyard.Ipfs.Engine.Migration;

/// <summary>
///     Allows migration of the repository.
/// </summary>
public class MigrationManager
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(MigrationManager));

    private readonly IpfsEngine _ipfs;

    /// <summary>
    ///     Creates a new instance of the <see cref="MigrationManager" /> class
    ///     for the specified <see cref="IpfsEngine" />.
    /// </summary>
    public MigrationManager(IpfsEngine ipfs)
    {
        _ipfs = ipfs;

        Migrations = typeof(MigrationManager).Assembly
            .GetTypes()
            .Where(x => typeof(IMigration).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .Select(x => (IMigration)Activator.CreateInstance(x))
            .OrderBy(x => x?.Version)
            .ToList();
    }

    /// <summary>
    ///     The list of migrations that can be performed.
    /// </summary>
    public List<IMigration> Migrations { get; }

    /// <summary>
    ///     Gets the latest supported version number of a repository.
    /// </summary>
    public int LatestVersion => Migrations.Last().Version;

    /// <summary>
    ///     Gets the current version number of the  repository.
    /// </summary>
    public int CurrentVersion
    {
        get
        {
            var path = VersionPath();
            if (!File.Exists(path))
            {
                return 0;
            }

            using var reader = new StreamReader(path);
            var s = reader.ReadLine();
            ArgumentException.ThrowIfNullOrEmpty(s);
            return int.Parse(s, CultureInfo.InvariantCulture);

        }
        private set => File.WriteAllText(VersionPath(), value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///     Upgrade/downgrade to the specified version.
    /// </summary>
    /// <param name="version">
    ///     The required version of the repository.
    /// </param>
    /// <param name="cancel">
    /// </param>
    /// <returns></returns>
    public async Task MigrateToVersionAsync(int version, CancellationToken cancel = default)
    {
        if (version != 0 && Migrations.All(m => m.Version != version))
        {
            throw new ArgumentOutOfRangeException(nameof(version), $"Repository version '{version}' is unknown.");
        }

        var currentVersion = CurrentVersion;
        var increment = CurrentVersion < version ? 1 : -1;
        while (currentVersion != version)
        {
            var nextVersion = currentVersion + increment;
            Log.InfoFormat("Migrating to version {0}", nextVersion);

            switch (increment)
            {
                case > 0:
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == nextVersion);
                    if (migration?.CanUpgrade ?? false)
                    {
                        await migration.UpgradeAsync(_ipfs, cancel);
                    }

                    break;
                }
                case < 0:
                {
                    var migration = Migrations.FirstOrDefault(m => m.Version == currentVersion);
                    if (migration?.CanDowngrade ?? false)
                    {
                        await migration.DowngradeAsync(_ipfs, cancel);
                    }

                    break;
                }
            }

            CurrentVersion = nextVersion;
            currentVersion = nextVersion;
        }
    }

    /// <summary>
    ///     Gets the FQN of the version file.
    /// </summary>
    /// <returns>
    ///     The path to the version file.
    /// </returns>
    private string VersionPath()
    {
        return Path.Combine(_ipfs.Options.Repository.ExistingFolder(), "version");
    }
}