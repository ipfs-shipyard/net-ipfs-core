﻿using Ipfs.CoreApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;

namespace Ipfs.Server.HttpApi.V0
{
    /// <summary>
    ///   Stats for an object.
    /// </summary>
    public class ObjectStatDto
    {
        /// <summary>
        ///   The CID of the object.
        /// </summary>
        public string Hash;

        /// <summary>
        ///   Number of links.
        /// </summary>
        public int NumLinks { get; set; }

        /// <summary>
        ///   Size of the links segment.
        /// </summary>
        public long LinksSize { get; set; }

        /// <summary>
        ///   Size of the raw, encoded data.
        /// </summary>
        public long BlockSize { get; set; }

        /// <summary>
        ///   Siz of the data segment.
        /// </summary>
        public long DataSize { get; set; }

        /// <summary>
        ///   Size of object and its references
        /// </summary>
        public long CumulativeSize { get; set; }
    }

    /// <summary>
    ///  A link to a file.
    /// </summary>
    public class ObjectLinkDto
    {
        /// <summary>
        ///   The object name.
        /// </summary>
        public string Name;

        /// <summary>
        ///   The CID of the object.
        /// </summary>
        public string Hash;

        /// <summary>
        ///   The object size.
        /// </summary>
        public long Size;
    }

    /// <summary>
    ///   Link details on an object.
    /// </summary>
    public class ObjectLinkDetailDto
    {
        /// <summary>
        ///   The CID of the object.
        /// </summary>
        public string Hash;

        /// <summary>
        ///   Links to other objects.
        /// </summary>
        public IEnumerable<ObjectLinkDto> Links;
    }

    /// <summary>
    ///   Data and link details on an object.
    /// </summary>
    public class ObjectDataDetailDto : ObjectLinkDetailDto
    {
        /// <summary>
        ///   The object data encoded as UTF-8.
        /// </summary>
        public string Data;
    }

    /// <summary>
    ///   Manages the IPFS Merkle Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///   <note>
    ///   This is being obsoleted by <see cref="IDagApi"/>.
    ///   </note>
    /// </remarks>
    public class ObjectController : IpfsController
    {
        /// <summary>
        ///   Creates a new controller.
        /// </summary>
        public ObjectController(ICoreApi ipfs) : base(ipfs) { }


        /// <summary>
        ///   Create an object from a template.
        /// </summary>
        /// <param name="arg">
        ///   Template name. Must be "unixfs-dir".
        /// </param>
        [HttpGet, HttpPost, Route("object/new")]
        public async Task<ObjectLinkDetailDto> Create(
            string arg)
        {
            var node = await IpfsCore.Object.NewAsync(arg, Cancel);
            Immutable();
            return new ObjectLinkDetailDto
            {
                Hash = node.Id,
                Links = node.Links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///   Store a MerkleDAG node.
        /// </summary>
        /// <param name="file">
        ///   multipart/form-data.
        /// </param>
        /// <param name="inputenc">
        ///   "protobuf" or "json"
        /// </param>
        /// <param name="datafieldenc">
        ///   "text" or "base64"
        /// </param>
        /// <param name="pin">
        ///   Pin the object.
        /// </param>
        /// <returns></returns>
        [HttpPost("object/put")]
        public async Task<ObjectLinkDetailDto> Put(
            IFormFile file,
            string inputenc = "json",
            string datafieldenc = "text",
            bool pin = false
        )
        {
            if (datafieldenc != "text")  // TODO
                throw new NotImplementedException("Only datafieldenc = `text` is allowed.");

            DagNode node = null;
            switch (inputenc)
            {
                case "protobuf":
                    using (var stream = file.OpenReadStream())
                    {
                        var dag = new DagNode(stream);
                        node = await IpfsCore.Object.PutAsync(dag, Cancel);
                    }
                    break;

                case "json": // TODO
                default:
                    throw new ArgumentException("inputenc", $"Input encoding '{inputenc}' is not supported.");
            }

            if (pin)
            {
                await IpfsCore.Pin.AddAsync(node.Id, false, Cancel);
            }

            return new ObjectLinkDetailDto
            {
                Hash = node.Id,
                Links = node.Links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///   Get the data and links of an object.
        /// </summary>
        /// <param name="arg">
        ///   The object's CID.
        /// </param>
        /// <param name="dataEncoding">
        ///   The encoding of the object's data; "text" (default) or "base64".
        /// </param>
        [HttpGet, HttpPost, Route("object/get")]
        public async Task<ObjectDataDetailDto> Get(
            string arg,
            [ModelBinder(Name = "data-encoding")] string dataEncoding)
        {
            var node = await IpfsCore.Object.GetAsync(arg, Cancel);
            Immutable();
            var dto = new ObjectDataDetailDto
            {
                Hash = arg,
                Links = node.Links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
            switch (dataEncoding)
            {
                case "base64":
                    dto.Data = Convert.ToBase64String(node.DataBytes);
                    break;
                case "text":
                default:
                    dto.Data = Encoding.UTF8.GetString(node.DataBytes);
                    break;
            }
            return dto;
        }

        /// <summary>
        ///   Get the links of an object.
        /// </summary>
        /// <param name="arg">
        ///   The object's CID.
        /// </param>
        [HttpGet, HttpPost, Route("object/links")]
        public async Task<ObjectLinkDetailDto> Links(
            string arg)
        {
            var links = await IpfsCore.Object.LinksAsync(arg, Cancel);
            Immutable();
            return new ObjectLinkDetailDto
            {
                Hash = arg,
                Links = links.Select(link => new ObjectLinkDto
                {
                    Hash = link.Id,
                    Name = link.Name,
                    Size = link.Size
                })
            };
        }

        /// <summary>
        ///   Get the object's data.
        /// </summary>
        /// <param name="arg">
        ///   The object's CID or a path.
        /// </param>
        [HttpGet, HttpPost, Route("object/data")]
        [Produces("text/plain")]
        public async Task<IActionResult> Data(string arg)
        {
            var r = await IpfsCore.Generic.ResolveAsync(arg, true, Cancel);
            var cid = Cid.Decode(r.Remove(0, 6));  // strip '/ipfs/'.
            var stream = await IpfsCore.Object.DataAsync(cid, Cancel);

            return File(stream, "text/plain");
        }

        /// <summary>
        ///   Get the stats of an object.
        /// </summary>
        /// <param name="arg">
        ///   The object's CID.
        /// </param>
        [HttpGet, HttpPost, Route("object/stat")]
        public async Task<ObjectStatDto> Stat(
            string arg)
        {
            var info = await IpfsCore.Object.StatAsync(arg, Cancel);
            Immutable();
            return new ObjectStatDto
            {
                Hash = arg,
                BlockSize = info.BlockSize,
                CumulativeSize = info.CumulativeSize,
                DataSize = info.DataSize,
                LinksSize = info.LinkSize,
                NumLinks = info.LinkCount
            };
        }

    }
}
