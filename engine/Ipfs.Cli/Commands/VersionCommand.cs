using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Show version information")]
internal class VersionCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var info = await Parent.CoreApi.Generic.VersionAsync();
        return Parent.Output(app, info, null);
    }
}