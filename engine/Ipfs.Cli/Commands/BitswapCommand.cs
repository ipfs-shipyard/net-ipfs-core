using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage swapped blocks")]
[Subcommand(typeof(BitswapWantListCommand))]
[Subcommand(typeof(BitswapUnwantCommand))]
[Subcommand(typeof(BitswapLedgerCommand))]
[Subcommand(typeof(BitswapStatCommand))]
internal class BitswapCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Show blocks currently on the wantlist")]
internal class BitswapWantListCommand : CommandBase
{
    [Option("-p|--peer", Description = "Peer to show wantlist for. Default: self.")]
    public string PeerId { get; set; }

    private BitswapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peer = PeerId == null
            ? null
            : new MultiHash(PeerId);
        var cids = await program.CoreApi.Bitswap.WantsAsync(peer);
        return program.Output(app, cids, (data, writer) =>
        {
            foreach (var cid in data)
            {
                writer.WriteLine(cid.Encode());
            }
        });
    }
}

[Command(Description = "Remove a block from the wantlist")]
internal class BitswapUnwantCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the block")]
    [Required]
    public string Cid { get; set; }

    private BitswapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.Bitswap.UnwantAsync(Cid);
        return 0;
    }
}

[Command(Description = "Show the current ledger for a peer")]
internal class BitswapLedgerCommand : CommandBase
{
    [Argument(0, "peerid", "The PeerID (B58) of the ledger to inspect")]
    [Required]
    public string PeerId { get; set; }

    private BitswapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peer = new Peer { Id = PeerId };
        var ledger = await program.CoreApi.Bitswap.LedgerAsync(peer);
        return program.Output(app, ledger, null);
    }
}

[Command(Description = "Show bitswap information")]
internal class BitswapStatCommand : CommandBase
{
    private BitswapCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;

        var stats = await program.CoreApi.Stats.BitswapAsync();
        return program.Output(app, stats, null);
    }
}