using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.Tests.CoreApi
{
    [TestClass]
    public class ConfigApiTest
    {
        private const string apiAddress = "/ip4/127.0.0.1/tcp/";
        private const string gatewayAddress = "/ip4/127.0.0.1/tcp/";

        [TestMethod]
        public void Get_Entire_Config()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var config = ipfs.Config.GetAsync().Result;
            StringAssert.StartsWith(Extensions.Value<string>(config["Addresses"]["API"]), apiAddress);
        }

        [TestMethod]
        public void Get_Scalar_Key_Value()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var api = ipfs.Config.GetAsync("Addresses.API").Result;
            StringAssert.StartsWith(Extensions.Value<string>(api), apiAddress);
        }

        [TestMethod]
        public void Get_Object_Key_Value()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var addresses = ipfs.Config.GetAsync("Addresses").Result;
            StringAssert.StartsWith(Extensions.Value<string>(addresses["API"]), apiAddress);
            StringAssert.StartsWith(Extensions.Value<string>(addresses["Gateway"]), gatewayAddress);
        }

        [TestMethod]
        public void Keys_are_Case_Sensitive()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var api = ipfs.Config.GetAsync("Addresses.API").Result;
            StringAssert.StartsWith(Extensions.Value<string>(api), apiAddress);

            ExceptionAssert.Throws<Exception>(() => { var x = ipfs.Config.GetAsync("Addresses.api").Result; });
        }

        [TestMethod]
        public void Set_String_Value()
        {
            const string key = "foo";
            const string value = "foobar";
            IpfsClient ipfs = TestFixture.Ipfs;
            ipfs.Config.SetAsync(key, value).Wait();
            Assert.AreEqual<JToken>(value, ipfs.Config.GetAsync(key).Result);
        }

        [TestMethod]
        public void Set_JSON_Value()
        {
            const string key = "API.HTTPHeaders.Access-Control-Allow-Origin";
            JToken value = JToken.Parse("['http://example.io']");
            IpfsClient ipfs = TestFixture.Ipfs;
            ipfs.Config.SetAsync(key, value).Wait();
            Assert.AreEqual<JToken>("http://example.io", ipfs.Config.GetAsync(key).Result[0]);
        }

        [TestMethod]
        public async Task Replace_Entire_Config()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
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
}
