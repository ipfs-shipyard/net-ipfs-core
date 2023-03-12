using System.Reflection;
using Common.Logging;
using Common.Logging.Configuration;
using Common.Logging.Simple;
using Ipfs.Cli.Commands;
using IpfsShipyard.Ipfs.CoreApi;
using Ipfs.Engine;
using Ipfs.Http;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Ipfs.Cli;

[Command("csipfs")]
[VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
[Subcommand(
    typeof(InitCommand),
    typeof(AddCommand),
    typeof(CatCommand),
    typeof(GetCommand),
    typeof(LsCommand),
    typeof(RefsCommand),
    typeof(IdCommand),
    typeof(ObjectCommand),
    typeof(BlockCommand),
    typeof(FilesCommand),
    typeof(DaemonCommand),
    typeof(ResolveCommand),
    typeof(NameCommand),
    typeof(KeyCommand),
    typeof(DnsCommand),
    typeof(PinCommand),
    typeof(PubsubCommand),
    typeof(BootstrapCommand),
    typeof(SwarmCommand),
    typeof(DhtCommand),
    typeof(ConfigCommand),
    typeof(VersionCommand),
    typeof(ShutdownCommand),
    typeof(UpdateCommand),
    typeof(BitswapCommand),
    typeof(StatsCommand),
    typeof(RepoCommand)
)]
internal class Program : CommandBase
{
    private static bool _debugging;
    private static bool _tracing;

    private ICoreApi _coreApi;

    [Option("--api <url>", Description = "Use a specific API instance")]
    public string ApiUrl { get; set; } = IpfsClient.DefaultApiUri.ToString();

    [Option("-L|--local", Description = "Run the command locally, instead of using the daemon")]
    public bool UseLocalEngine { get; set; }

    [Option("--enc", Description = "The output type (json, xml, or text)")]
    public string OutputEncoding { get; set; } = "text";

    [Option("--debug", Description = "Show debugging info")]
    public bool Debug { get; set; } // just for documentation, already parsed in Main

    [Option("--trace", Description = "Show tracing info")]
    public bool Trace { get; set; } // just for documentation, already parsed in Main

    [Option("--time", Description = "Show how long the command took")]
    public bool ShowTime { get; set; }

    public ICoreApi CoreApi
    {
        get
        {
            if (_coreApi != null)
            {
                return _coreApi;
            }

            if (UseLocalEngine)
            {
                // TODO: Add option --pass
                var passphrase = "this is not a secure pass phrase";
                var engine = new IpfsEngine(passphrase.ToCharArray());
                engine.StartAsync().Wait();
                _coreApi = engine;
            }
            else
            {
                _coreApi = new IpfsClient(ApiUrl);
            }

            return _coreApi;
        }
    }

    public static int Main(string[] args)
    {
        var startTime = DateTime.Now;

        // Need to setup common.logging early.
        _debugging = args.Any(s => s == "--debug");
        _tracing = args.Any(s => s == "--trace");
        var properties = new NameValueCollection
        {
            ["level"] = _tracing ? "TRACE" : _debugging ? "DEBUG" : "OFF",
            ["showLogName"] = "true",
            ["showDateTime"] = "true",
            ["dateTimeFormat"] = "HH:mm:ss.fff"
        };
        LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

        try
        {
            CommandLineApplication.Execute<Program>(args);
        }
        catch (Exception e)
        {
            for (; e != null; e = e.InnerException)
            {
                Console.Error.WriteLine(e.Message);
                if (!_debugging && !_tracing)
                {
                    continue;
                }

                Console.WriteLine();
                Console.WriteLine(e.StackTrace);
            }

            return 1;
        }

        var took = DateTime.Now - startTime;
        //Console.Write($"Took {took.TotalSeconds} seconds.");

        return 0;
    }

    protected override Task<int> OnExecute(CommandLineApplication app)
    {
        app.ShowHelp();
        return Task.FromResult(0);
    }

    public int Output<T>(CommandLineApplication app, T data, Action<T, TextWriter> text)
        where T : class
    {
        OutputEncoding = text == null ? "json" : OutputEncoding;

        switch (OutputEncoding.ToLowerInvariant())
        {
            case "text":
                text?.Invoke(data, app.Out);

                break;

            case "json":
                var x = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                x.Serialize(app.Out, data);
                break;

            default:
                app.Error.WriteLine($"Unknown output encoding '{OutputEncoding}'");
                return 1;
        }

        return 0;
    }

    private static string GetVersion() =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
}