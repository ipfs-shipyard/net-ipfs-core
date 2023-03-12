using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.Ipfs.Core.CoreApi;
using Newtonsoft.Json.Linq;

namespace IpfsShipyard.Ipfs.Http.CoreApi;

class KeyApi : IKeyApi
{
    /// <summary>
    ///   Information about a local key.
    /// </summary>
    public class KeyInfo : IKey
    {
        /// <inheritdoc />
        public MultiHash Id { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

    }

    readonly IpfsClient _ipfs;

    internal KeyApi(IpfsClient ipfs)
    {
        _ipfs = ipfs;
    }

    public async Task<IKey> CreateAsync(string name, string keyType, int size, CancellationToken cancel = default(CancellationToken))
    {
        return await _ipfs.DoCommandAsync<KeyInfo>("key/gen", cancel,
            name,
            $"type={keyType}",
            $"size={size}");
    }

    public async Task<IEnumerable<IKey>> ListAsync(CancellationToken cancel = default(CancellationToken))
    {
        var json = await _ipfs.DoCommandAsync("key/list", cancel, null, "l=true");
        var keys = (JArray)(JObject.Parse(json)["Keys"]);
        return keys
            .Select(k => new KeyInfo
            {
                Id = (string)k["Id"],
                Name = (string)k["Name"]
            });
    }

    public async Task<IKey> RemoveAsync(string name, CancellationToken cancel = default(CancellationToken))
    {
        var json = await _ipfs.DoCommandAsync("key/rm", cancel, name);
        var keys = JObject.Parse(json)["Keys"] as JArray;

        return keys?
            .Select(k => new KeyInfo
            {
                Id = (string)k["Id"],
                Name = (string)k["Name"]
            })
            .First();
    }

    public async Task<IKey> RenameAsync(string oldName, string newName, CancellationToken cancel = default(CancellationToken))
    {
        var json = await _ipfs.DoCommandAsync("key/rename", cancel, oldName, $"arg={newName}");
        var key = JObject.Parse(json);
        return new KeyInfo
        {
            Id = (string)key["Id"],
            Name = (string)key["Now"]
        };
    }

    public Task<string> ExportAsync(string name, char[] password, CancellationToken cancel = default(CancellationToken))
    {
        throw new NotImplementedException();
    }

    public Task<IKey> ImportAsync(string name, string pem, char[] password = null, CancellationToken cancel = default(CancellationToken))
    {
        throw new NotImplementedException();
    }
}