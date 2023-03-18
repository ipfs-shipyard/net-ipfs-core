using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Query IPFS statistics")]
[Subcommand(typeof(StatsBandwidthCommand),
    typeof(StatsRepoCommand),
    typeof(StatsBitswapCommand))]
internal class StatsCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "IPFS bandwidth information")]
internal class StatsBandwidthCommand : CommandBase
{
    private StatsCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.Stats.BandwidthAsync();
        return program.Output(app, stats, null);
    }
}

[Command(Description = "Repository information")]
internal class StatsRepoCommand : CommandBase
{
    private StatsCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.Stats.RepositoryAsync();
        return program.Output(app, stats, null);
    }
}

[Command(Description = "Bitswap information")]
internal class StatsBitswapCommand : CommandBase
{
    private StatsCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.Stats.BitswapAsync();
        return program.Output(app, stats, null);
    }
}