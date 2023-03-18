using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Manage IPNS names")]
[Subcommand(typeof(NamePublishCommand),
    typeof(NameResolveCommand))]
internal class NameCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Resolve a name")]
internal class NameResolveCommand : CommandBase
{
    private NameCommand Parent { get; set; }

    [Argument(0, "name", "A key or a DNS name")]
    [Required]
    public string Name { get; set; }

    [Option("-r|--recursive", Description = "Resolve until the result is an IPFS name")]
    public bool Recursive { get; set; }

    [Option("-n|--nocache", Description = "Do not use cached entries")]
    public bool NoCache { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var resolved = await program.CoreApi.Name.ResolveAsync(Name, Recursive, NoCache);
        await app.Out.WriteAsync(resolved);
        return 0;
    }
}

[Command(Description = "Publish a name")]
internal class NamePublishCommand : CommandBase
{
    private NameCommand Parent { get; set; }

    [Argument(0, "ipfs-path", "The path to the IPFS data")]
    [Required]
    public string IpfsPath { get; set; }

    [Option("-r|--recursive", Description = "Resolve until the result is an IPFS name")]
    public bool Recursive { get; set; }

    [Option("-k|--key", Description = "The key name, defaults to 'self'.")]
    public string Key { get; set; }

    // TODO: Lifetime

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var content = await program.CoreApi.Name.PublishAsync(IpfsPath, true, Key);
        return program.Output(app, content, (data, writer) => { writer.Write($"Published to {data.NamePath}"); });
    }
}