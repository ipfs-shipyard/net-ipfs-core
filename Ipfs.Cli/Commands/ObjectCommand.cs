using System.ComponentModel.DataAnnotations;
using Ipfs.Engine.UnixFileSystem;
using IpfsShipyard.Ipfs.Core;
using McMaster.Extensions.CommandLineUtils;
using ProtoBuf;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage IPFS objects")]
[Subcommand(typeof(ObjectLinksCommand),
    typeof(ObjectGetCommand),
    typeof(ObjectDumpCommand),
    typeof(ObjectStatCommand))]
internal class ObjectCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "Information on the links pointed to by the IPFS block")]
internal class ObjectLinksCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the object")]
    [Required]
    public string Cid { get; set; }

    private ObjectCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var links = await program.CoreApi.Object.LinksAsync(Cid);

        return program.Output(app, links, (data, writer) =>
        {
            foreach (var link in data)
            {
                writer.WriteLine($"{link.Id.Encode()} {link.Size} {link.Name}");
            }
        });
    }
}

[Command(Description = "Serialise the DAG node")]
internal class ObjectGetCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the object")]
    [Required]
    public string Cid { get; set; }

    private ObjectCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var node = await program.CoreApi.Object.GetAsync(Cid);

        return program.Output(app, node, null);
    }
}

[Command(Description = "Dump the DAG node")]
internal class ObjectDumpCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the object")]
    [Required]
    public string Cid { get; set; }

    private ObjectCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var node = new Node();
        var block = await program.CoreApi.Block.GetAsync(Cid);
        node.Dag = new(block.DataStream);
        node.DataMessage = Serializer.Deserialize<DataMessage>(node.Dag.DataStream);

        return program.Output(app, node, null);
    }

    private class Node
    {
        public DagNode Dag;
        public DataMessage DataMessage;
    }
}

[Command(Description = "Stats for the DAG node")]
internal class ObjectStatCommand : CommandBase
{
    [Argument(0, "cid", "The content ID of the object")]
    [Required]
    public string Cid { get; set; }

    private ObjectCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var stat = await program.CoreApi.Object.StatAsync(Cid);

        return program.Output(app, stat, null);
    }
}