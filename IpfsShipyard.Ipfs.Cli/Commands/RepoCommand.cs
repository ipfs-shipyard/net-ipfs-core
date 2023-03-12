using System.ComponentModel.DataAnnotations;
using IpfsShipyard.Ipfs.Engine;
using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Manage the IPFS repository")]
[Subcommand(typeof(RepoGcCommand),
    typeof(RepoMigrateCommand),
    typeof(RepoStatCommand),
    typeof(RepoVerifyCommand),
    typeof(RepoVersionCommand))]
internal class RepoCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Perform a garbage collection sweep on the repo")]
internal class RepoGcCommand : CommandBase
{
    private RepoCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        await program.CoreApi.BlockRepository.RemoveGarbageAsync();
        return 0;
    }
}

[Command(Description = "Verify all blocks in repo are not corrupted")]
internal class RepoVerifyCommand : CommandBase
{
    private RepoCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        await program.CoreApi.BlockRepository.VerifyAsync();
        return 0;
    }
}

[Command(Description = "Repository information")]
internal class RepoStatCommand : CommandBase
{
    private RepoCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.BlockRepository.StatisticsAsync();
        return program.Output(app, stats, null);
    }
}

[Command(Description = "Repository version")]
internal class RepoVersionCommand : CommandBase
{
    private RepoCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.BlockRepository.VersionAsync();
        return program.Output(app, stats, null);
    }
}

[Command(Description = "Migrate to the version")]
internal class RepoMigrateCommand : CommandBase
{
    private RepoCommand Parent { get; set; }

    [Argument(0, "version", "The version number of the repository")]
    [Required]
    public int Version { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        // TODO: Add option --pass
        var passphrase = "this is not a secure pass phrase";
        var ipfs = new IpfsEngine(passphrase.ToCharArray());

        await ipfs.MigrationManager.MigrateToVersionAsync(Version);
        return 0;
    }
}