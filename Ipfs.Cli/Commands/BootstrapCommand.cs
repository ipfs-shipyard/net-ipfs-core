using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage bootstrap peers")]
[Subcommand(typeof(BootstrapListCommand),
    typeof(BootstrapRemoveCommand),
    typeof(BootstrapAddCommand))]
internal class BootstrapCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "List the bootstrap peers")]
internal class BootstrapListCommand : CommandBase
{
    private BootstrapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peers = await program.CoreApi.Bootstrap.ListAsync();
        return program.Output(app, peers, (data, writer) =>
        {
            foreach (var addresss in data)
            {
                writer.WriteLine(addresss);
            }
        });
    }
}

[Command(Description = "Add the bootstrap peer")]
[Subcommand(typeof(BootstrapAddDefaultCommand))]
internal class BootstrapAddCommand : CommandBase
{
    [Argument(0, "addr", "A multiaddress to the peer")]
    public string Address { get; set; }

    public BootstrapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.Bootstrap.AddAsync(Address);
        return 0;
    }
}

[Command(Description = "Add the default bootstrap peers")]
internal class BootstrapAddDefaultCommand : CommandBase
{
    private BootstrapAddCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent.Parent;
        var peers = await program.CoreApi.Bootstrap.AddDefaultsAsync();
        return program.Output(app, peers, (data, writer) =>
        {
            foreach (var a in data)
            {
                writer.WriteLine(a);
            }
        });
    }
}

[Command(Description = "Remove the bootstrap peer")]
[Subcommand(typeof(BootstrapRemoveAllCommand))]
internal class BootstrapRemoveCommand : CommandBase
{
    [Argument(0, "addr", "A multiaddress to the peer")]
    public string Address { get; set; }

    public BootstrapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.Bootstrap.RemoveAsync(Address);
        return 0;
    }
}

[Command(Description = "Remove all the bootstrap peers")]
internal class BootstrapRemoveAllCommand : CommandBase
{
    private BootstrapRemoveCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent.Parent;
        await program.CoreApi.Bootstrap.RemoveAllAsync();
        return 0;
    }
}