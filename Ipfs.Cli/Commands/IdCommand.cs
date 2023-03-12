using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Ipfs.Cli.Commands;

[Command(Description = "Show info on an IPFS peer")]
internal class IdCommand : CommandBase
{
    [Argument(0, "peerid", "The IPFS peer ID")]
    public string PeerId { get; set; }

    private Program Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var id = PeerId == null ? null : new MultiHash(PeerId);
        var peer = await Parent.CoreApi.Generic.IdAsync(id);
        await using JsonWriter writer = new JsonTextWriter(app.Out);
        writer.Formatting = Formatting.Indented;

        await writer.WriteStartObjectAsync();
        await writer.WritePropertyNameAsync("ID");
        await writer.WriteValueAsync(peer.Id.ToBase58());
        await writer.WritePropertyNameAsync("PublicKey");
        await writer.WriteValueAsync(peer.PublicKey);
        await writer.WritePropertyNameAsync("Adddresses");
        await writer.WriteStartArrayAsync();
        foreach (var a in peer.Addresses)
        {
            if (a != null)
            {
                await writer.WriteValueAsync(a.ToString());
            }
        }

        await writer.WriteEndArrayAsync();
        await writer.WritePropertyNameAsync("AgentVersion");
        await writer.WriteValueAsync(peer.AgentVersion);
        await writer.WritePropertyNameAsync("ProtocolVersion");
        await writer.WriteValueAsync(peer.ProtocolVersion);
        await writer.WriteEndObjectAsync();
        return 0;
    }
}