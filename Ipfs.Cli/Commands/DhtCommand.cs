using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Query the DHT for values or peers")]
[Subcommand(typeof(DhtFindPeerCommand),
    typeof(DhtFindProvidersCommand))]
internal class DhtCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Find the multiaddresses associated with the peer ID")]
internal class DhtFindPeerCommand : CommandBase
{
    private DhtCommand Parent { get; set; }

    [Argument(0, "peerid", "The IPFS peer ID")]
    [Required]
    public string PeerId { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var peer = await program.CoreApi.Dht.FindPeerAsync(new(PeerId));
        return program.Output(app, peer, (data, writer) =>
        {
            foreach (var a in peer.Addresses)
            {
                writer.WriteLine(a.ToString());
            }
        });
    }
}

[Command(Description = "Find peers that can provide a specific value, given a key")]
internal class DhtFindProvidersCommand : CommandBase
{
    private DhtCommand Parent { get; set; }

    [Argument(0, "key", "The multihash key or a CID")]
    [Required]
    public string Key { get; set; }

    [Option("-n|--num-providers", Description = "The number of providers to find")]
    public int Limit { get; set; } = 20;

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var peers = await program.CoreApi.Dht.FindProvidersAsync(Cid.Decode(Key), Limit);
        var enumerable = peers.ToList();
        return program.Output(app, enumerable, (data, writer) =>
        {
            foreach (var peer in enumerable)
            {
                writer.WriteLine(peer.Id.ToString());
            }
        });
    }
}