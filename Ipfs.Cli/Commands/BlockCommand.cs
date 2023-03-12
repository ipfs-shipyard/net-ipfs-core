using System.ComponentModel.DataAnnotations;
using IpfsShipyard.Ipfs.Core;
using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage raw blocks")]
[Subcommand(typeof(BlockStatCommand),
    typeof(BlockRemoveCommand),
    typeof(BlockGetCommand),
    typeof(BlockPutCommand))]
internal class BlockCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command("rm", Description = "Remove the IPFS block")]
internal class BlockRemoveCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the block")]
    [Required]
    public string Cid { get; set; }

    [Option("-f|-force", Description = "Ignore nonexistent blocks")]
    public bool Force { get; set; }

    private BlockCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var cid = await program.CoreApi.Block.RemoveAsync(Cid, Force);

        return program.Output(app, cid, (data, writer) => { writer.WriteLine($"Removed {data.Encode()}"); });
    }
}

[Command("stat", Description = "Information on on the IPFS block")]
internal class BlockStatCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the block")]
    [Required]
    public string Cid { get; set; }

    private BlockCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var block = await program.CoreApi.Block.StatAsync(Cid);

        return program.Output(app, block, (data, writer) => { writer.WriteLine($"{data.Id.Encode()} {data.Size}"); });
    }
}

[Command("get", Description = "Get the IPFS block")]
internal class BlockGetCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the block")]
    [Required]
    public string Cid { get; set; }

    private BlockCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var block = await program.CoreApi.Block.GetAsync(Cid);
        await block.DataStream.CopyToAsync(Console.OpenStandardOutput());

        return 0;
    }
}

[Command("put", Description = "Put the IPFS block")]
internal class BlockPutCommand : CommandBase
{
    [Argument(0, "path", "The file containing the data")]
    [Required]
    public string BlockPath { get; set; }

    [Option("--hash", Description = "The hashing algorithm")]
    public string MultiHashType { get; set; } = MultiHash.DefaultAlgorithmName;

    [Option("--pin", Description = "Pin the block")]
    public bool Pin { get; set; }

    private BlockCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var blockData = await File.ReadAllBytesAsync(BlockPath);
        var cid = await program.CoreApi.Block.PutAsync
        (
            blockData,
            pin: Pin,
            multiHash: MultiHashType
        );

        return program.Output(app, cid, (data, writer) => { writer.WriteLine($"Added {data.Encode()}"); });
    }
}