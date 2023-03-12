using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi
{
    class DnsApi : IDnsApi
    {
        private Http.IpfsClient ipfs;

        internal DnsApi(Http.IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        public async Task<string> ResolveAsync(string name, bool recursive = false, CancellationToken cancel = default(CancellationToken))
        {
            var json = await ipfs.DoCommandAsync("dns", cancel,
                name,
                $"recursive={recursive.ToString().ToLowerInvariant()}");
            var path = (string)(JObject.Parse(json)["Path"]);
            return path;
        }
    }
}
