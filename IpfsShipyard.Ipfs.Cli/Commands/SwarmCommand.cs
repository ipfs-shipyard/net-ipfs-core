using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Manage connections to the p2p network")]
[Subcommand(typeof(SwarmConnectCommand),
    typeof(SwarmDisconnectCommand),
    typeof(SwarmPeersCommand),
    typeof(SwarmAddrsCommand))]
internal class SwarmCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Connect to a peer")]
internal class SwarmConnectCommand : CommandBase
{
    [Argument(0, "addr", "A multiaddress to the peer")]
    [Required]
    public string Address { get; set; }

    public SwarmCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.Swarm.ConnectAsync(Address);
        return 0;
    }
}

[Command(Description = "Disconnect from a peer")]
internal class SwarmDisconnectCommand : CommandBase
{
    [Argument(0, "addr", "A multiaddress to the peer")]
    [Required]
    public string Address { get; set; }

    public SwarmCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.Swarm.DisconnectAsync(Address);
        return 0;
    }
}

[Command(Description = "List of connected peers")]
internal class SwarmPeersCommand : CommandBase
{
    public SwarmCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peers = await program.CoreApi.Swarm.PeersAsync();
        program.Output(app, peers, (data, writer) =>
        {
            foreach (var peer in data)
            {
                writer.WriteLine(peer.ConnectedAddress);
            }
        });
        return 0;
    }
}

[Command(Description = "List addresses of known peers")]
internal class SwarmAddrsCommand : CommandBase
{
    public SwarmCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peers = await program.CoreApi.Swarm.AddressesAsync();
        program.Output(app, peers, (data, writer) =>
        {
            foreach (var address in data.SelectMany(p => p.Addresses))
            {
                writer.WriteLine(address);
            }
        });
        return 0;
    }
}