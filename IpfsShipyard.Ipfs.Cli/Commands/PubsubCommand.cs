using System.ComponentModel.DataAnnotations;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace IpfsShipyard.Ipfs.Cli.Commands;

[Command(Description = "Publish/subscribe to messages on a given topic")]
[Subcommand(typeof(PubsubListCommand),
    typeof(PubsubPeersCommand),
    typeof(PubsubPublishCommand),
    typeof(PubsubSubscribeCommand))]
internal class PubsubCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "List subscribed topics by name")]
internal class PubsubListCommand : CommandBase
{
    private PubsubCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var topics = await program.CoreApi.PubSub.SubscribedTopicsAsync();
        var enumerable = topics.ToList();
        return program.Output(app, enumerable, (data, writer) =>
        {
            foreach (var topic in enumerable)
            {
                writer.WriteLine(topic);
            }
        });
    }
}

[Command(Description = "List peers that are pubsubbing with")]
internal class PubsubPeersCommand : CommandBase
{
    [Argument(0, "topic", "The topic of interest")]
    public string Topic { get; set; }

    private PubsubCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var peers = await program.CoreApi.PubSub.PeersAsync(Topic);
        return program.Output(app, peers, null);
    }
}

[Command(Description = "Publish a message on a topic")]
internal class PubsubPublishCommand : CommandBase
{
    [Argument(0, "topic", "The topic of interest")]
    [Required]
    public string Topic { get; set; }

    [Argument(1, "message", "The data to publish")]
    [Required]
    public string Message { get; set; }

    private PubsubCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        await program.CoreApi.PubSub.PublishAsync(Topic, Message);
        return 0;
    }
}

[Command(Description = "Subscribe to messages on a topic")]
internal class PubsubSubscribeCommand : CommandBase
{
    [Argument(0, "topic", "The topic of interest")]
    [Required]
    public string Topic { get; set; }

    private PubsubCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var cts = new CancellationTokenSource();
        await program.CoreApi.PubSub.SubscribeAsync(Topic,
            m =>
            {
                program.Output(app, m,
                    (data, writer) => { writer.WriteLine(Encoding.UTF8.GetString(data.DataBytes)); });
            }, cts.Token);

        // Never return, just print messages received.
        await Task.Delay(-1, cts.Token);

        // Keep compiler happy.
        return 0;
    }
}