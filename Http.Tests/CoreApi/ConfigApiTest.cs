using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi;

[TestClass]
public class ConfigApiTest
{
    private const string ApiAddress = "/ip4/127.0.0.1/tcp/";
    private const string GatewayAddress = "/ip4/127.0.0.1/tcp/";

    [TestMethod]
    public void Get_Entire_Config()
    {
        var ipfs = TestFixture.Ipfs;
        var config = ipfs.Config.GetAsync().Result;
        StringAssert.StartsWith(config["Addresses"]["API"].Value<string>(), ApiAddress);
    }

    [TestMethod]
    public void Get_Scalar_Key_Value()
    {
        var ipfs = TestFixture.Ipfs;
        var api = ipfs.Config.GetAsync("Addresses.API").Result;
        StringAssert.StartsWith(api.Value<string>(), ApiAddress);
    }

    [TestMethod]
    public void Get_Object_Key_Value()
    {
        var ipfs = TestFixture.Ipfs;
        var addresses = ipfs.Config.GetAsync("Addresses").Result;
        StringAssert.StartsWith(addresses["API"].Value<string>(), ApiAddress);
        StringAssert.StartsWith(addresses["Gateway"].Value<string>(), GatewayAddress);
    }

    [TestMethod]
    public void Keys_are_Case_Sensitive()
    {
        var ipfs = TestFixture.Ipfs;
        var api = ipfs.Config.GetAsync("Addresses.API").Result;
        StringAssert.StartsWith(api.Value<string>(), ApiAddress);

        ExceptionAssert.Throws<Exception>(() => { var x = ipfs.Config.GetAsync("Addresses.api").Result; });
    }

    [TestMethod]
    public void Set_String_Value()
    {
        const string key = "foo";
        const string value = "foobar";
        var ipfs = TestFixture.Ipfs;
        ipfs.Config.SetAsync(key, value).Wait();
        Assert.AreEqual(value, ipfs.Config.GetAsync(key).Result);
    }

    [TestMethod]
    public void Set_JSON_Value()
    {
        const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
        var value = JToken.Parse("['http://example.io']");
        var ipfs = TestFixture.Ipfs;
        ipfs.Config.SetAsync(key, value).Wait();
        Assert.AreEqual("http://example.io", ipfs.Config.GetAsync(key).Result[0]);
    }

    [TestMethod]
    public async Task Replace_Entire_Config()
    {
        var ipfs = TestFixture.Ipfs;
        var original = await ipfs.Config.GetAsync();
        try
        {
            var a = JObject.Parse("{ \"foo-x-bar\": 1 }");
            await ipfs.Config.ReplaceAsync(a);
        }
        finally
        {
            await ipfs.Config.ReplaceAsync(original);
        }
    }

}