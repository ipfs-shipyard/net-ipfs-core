using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage the configuration")]
[Subcommand(typeof(ConfigShowCommand),
    typeof(ConfigReplaceCommand))]
internal class ConfigCommand : CommandBase
{
    public Program Parent { get; set; }

    [Argument(0, "name", "The name of the configuration setting")]
    public string Name { get; set; }

    [Argument(1, "value", "The value of the configuration setting")]
    public string Value { get; set; }

    [Option("--json", Description = "Treat the <value> as JSON")]
    public bool ValueIsJson { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        if (Name == null)
        {
            app.ShowHelp();
            return 0;
        }

        if (Value == null)
        {
            var json = await Parent.CoreApi.Config.GetAsync(Name);
            await app.Out.WriteAsync(json.ToString());
            return 0;
        }

        if (ValueIsJson)
        {
            var value = JToken.Parse(Value);
            await Parent.CoreApi.Config.SetAsync(Name, value);
        }
        else
        {
            await Parent.CoreApi.Config.SetAsync(Name, Value);
        }

        return 0;
    }
}

[Command(Description = "Show the config file contents")]
internal class ConfigShowCommand : CommandBase
{
    private ConfigCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var json = await program.CoreApi.Config.GetAsync();
        await app.Out.WriteAsync(json.ToString());
        return 0;
    }
}

[Command(Description = "Replace the config file")]
internal class ConfigReplaceCommand : CommandBase
{
    [Argument(0, "path", "The path to the config file")]
    [Required]
    public string FilePath { get; set; }

    private ConfigCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var json = JObject.Parse(await File.ReadAllTextAsync(FilePath));
        await program.CoreApi.Config.ReplaceAsync(json);
        return 0;
    }
}