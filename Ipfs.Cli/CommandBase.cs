using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli;

[HelpOption("--help")]
internal abstract class CommandBase
{
    protected virtual Task<int> OnExecute(CommandLineApplication app)
    {
        app.Error.WriteLine($"The command '{app.Name}' is not implemented yet.");
        return Task.FromResult(1);
    }
}