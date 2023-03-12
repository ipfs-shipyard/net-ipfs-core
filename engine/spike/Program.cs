using Common.Logging;
using Common.Logging.Configuration;
using Ipfs.Engine.Tests.CoreApi;

namespace spike;

internal class Program
{
    private static void Main(string[] args)
    {
        // set logger factory
        var properties = new NameValueCollection
        {
            ["level"] = "DEBUG",
            ["showLogName"] = "true",
            ["showDateTime"] = "true",
            ["dateTimeFormat"] = "HH:mm:ss.fff"
        };
        LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter(properties);

        var test = new BitswapApiTest();
        test.GetsBlock_OnConnect().Wait();
    }
}