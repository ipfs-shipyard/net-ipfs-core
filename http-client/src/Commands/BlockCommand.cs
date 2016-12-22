﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ipfs.Api
{

    public class Block
    {
        public string Hash { get; set; }
        public byte[] Data { get; set; }
    }

    public class BlockInfo
    {
        public string Key { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    ///   Manages raw <see cref="Block">IPFS blocks</see>.
    /// </summary>
    /// <remarks>
    ///   This API is accessed via the <see cref="IpfsClient.Block"/> property.
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/tree/master/API/block">Block API</seealso>
    public class BlockCommand
    {
        IpfsClient ipfs;

        internal BlockCommand(IpfsClient ipfs)
        {
            this.ipfs = ipfs;
        }

        /// <summary>
        ///   Gets a raw <see cref="Block">IPFS block</see>.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of a base58 encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        public async Task<Block> GetAsync(string hash) // TODO CID support
        {
            var data = await ipfs.DownloadBytesAsync("block/get", hash);
            return new Block
            {
                Data = data,
                Hash = hash
            };
        }

        /// <summary>
        ///   Stores a byte array as a raw <see cref="Block">IPFS block</see>.
        /// </summary>
        /// <param name="data">
        ///   The byte array to send to the IPFS network.
        /// </param>
        public async Task<Block> PutAsync(byte[] data)
        {
            var json = await ipfs.UploadAsync("block/put", data);
            var info = JsonConvert.DeserializeObject<BlockInfo>(json);
            return new Block
            {
                Data = data,
                Hash = info.Key
            };
        }

        /// <summary>
        ///   Stores a raw <see cref="Block">IPFS block</see>.
        /// </summary>
        /// <param name="block">
        ///   The <seealso cref="Block"/> to send to the IPFS network.
        /// </param>
        public Task<Block> PutAsync(Block block)
        {
            return PutAsync(block.Data);
        }

        /// <summary>
        ///   Information on a raw <see cref="Block">IPFS block</see>.
        /// </summary>
        /// <param name="hash">
        ///   The <see cref="string"/> representation of a base58 encoded <see cref="Ipfs.MultiHash"/>.
        /// </param>
        public Task<BlockInfo> StatAsync(string hash)
        {
            return ipfs.DoCommandAsync<BlockInfo>("block/stat", hash);
        }
    }

}
