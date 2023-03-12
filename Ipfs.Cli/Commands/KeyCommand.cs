using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;

namespace Ipfs.Cli.Commands;

[Command(Description = "Manage private keys")]
[Subcommand(typeof(KeyListCommand),
    typeof(KeyRemoveCommand),
    typeof(KeyCreateCommand),
    typeof(KeyRenameCommand),
    typeof(KeyExportCommand),
    typeof(KeyImportCommand))]
internal class KeyCommand : CommandBase
{
    public Program Parent { get; set; }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }
}

[Command(Description = "List the keys")]
internal class KeyListCommand : CommandBase
{
    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var keys = await program.CoreApi.Key.ListAsync();
        return program.Output(app, keys, (data, writer) =>
        {
            foreach (var key in data)
            {
                writer.WriteLine($"{key.Id} {key.Name}");
            }
        });
    }
}

[Command(Description = "Remove the key")]
internal class KeyRemoveCommand : CommandBase
{
    [Argument(0, "name", "The name of the key")]
    [Required]
    public string Name { get; set; }

    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var key = await program.CoreApi.Key.RemoveAsync(Name);
        if (key != null)
        {
            return program.Output(app, key, (data, writer) => { writer.WriteLine($"Removed {data.Id} {data.Name}"); });
        }

        await app.Error.WriteLineAsync($"The key '{Name}' is not defined.");
        return 1;

    }
}

[Command(Description = "Rename the key")]
internal class KeyRenameCommand : CommandBase
{
    [Argument(0, "name", "The name of the key")]
    [Required]
    public string Name { get; set; }

    [Argument(1, "new-name", "The new name of the key")]
    [Required]
    public string NewName { get; set; }

    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var key = await program.CoreApi.Key.RenameAsync(Name, NewName);
        if (key != null)
        {
            return program.Output(app, key, (data, writer) => { writer.WriteLine($"Renamed to {data.Name}"); });
        }

        await app.Error.WriteLineAsync($"The key '{Name}' is not defined.");
        return 1;

    }
}

[Command(Description = "Create a key")]
internal class KeyCreateCommand : CommandBase
{
    [Argument(0, "name", "The name of the key")]
    [Required]
    public string Name { get; set; }

    [Option("-t|--type", Description = "The type of the key [rsa, ed25519]")]
    public string KeyType { get; set; } = "rsa";

    [Option("-s|--size", Description = "The key size")]
    public int KeySize { get; set; }

    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var key = await program.CoreApi.Key.CreateAsync(Name, KeyType, KeySize);
        return program.Output(app, key, (data, writer) => { writer.WriteLine($"{data.Id} {data.Name}"); });
    }
}

[Command(Description = "Export the key to a PKCS #8 PEM file")]
internal class KeyExportCommand : CommandBase
{
    [Argument(0, "name", "The name of the key")]
    [Required]
    public string Name { get; set; }

    [Option("-o|--output", Description = "The file name for the PEM file")]
    public string OutputBasePath { get; set; }

    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var pass = Prompt.GetPassword("Password for PEM file?");
        var pem = await program.CoreApi.Key.ExportAsync(Name, pass.ToCharArray());
        if (OutputBasePath == null)
        {
            await app.Out.WriteAsync(pem);
        }
        else
        {
            var path = OutputBasePath;
            if (!Path.HasExtension(path))
            {
                path = Path.ChangeExtension(path, ".pem");
            }
            ArgumentException.ThrowIfNullOrEmpty(path);

            await using var writer = File.CreateText(path);
            await writer.WriteAsync(pem);
        }

        return 0;
    }
}

[Command(Description = "Import the key from a PKCS #8 PEM file")]
internal class KeyImportCommand : CommandBase
{
    [Argument(0, "name", "The name of the key")]
    [Required]
    public string Name { get; set; }

    [Argument(1, "path", "The path to the PEM file")]
    [Required]
    public string PemPath { get; set; }

    private KeyCommand Parent { get; set; }

    protected override async Task<int> OnExecute(CommandLineApplication app)
    {
        var program = Parent.Parent;
        var pem = await File.ReadAllTextAsync(PemPath);
        var pass = Prompt.GetPassword("Password for PEM file?");
        var key = await program.CoreApi.Key.ImportAsync(Name, pem, pass.ToCharArray());
        return program.Output(app, key, (data, writer) => { writer.WriteLine($"{data.Id} {data.Name}"); });
    }
}