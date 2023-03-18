using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.Ipfs.Http;

/// <inheritdoc />
[DataContract]
public class FileSystemNode : IFileSystemNode
{
    private IpfsClient _ipfsClient;
    private IEnumerable<IFileSystemLink> _links;
    private long? _size;
    private bool? _isDirectory;

    /// <inheritdoc />
    public byte[] DataBytes
    {
        get
        {
            using var stream = DataStream;
            if (DataStream == null)
                return null;

            using var data = new MemoryStream();
            stream.CopyTo(data);
            return data.ToArray();
        }
    }

    /// <inheritdoc />
    public Stream DataStream => IpfsClient?.FileSystem.ReadFileAsync(Id).Result;

    /// <inheritdoc />
    [DataMember]
    public Cid Id { get; set; }

    /// <inheritdoc />
    [DataMember]
    public IEnumerable<IFileSystemLink> Links
    {
        get
        {
            if (_links == null) GetInfo();
            return _links;
        }
        set => _links = value;
    }

    /// <summary>
    ///   Size of the file contents.
    /// </summary>
    /// <value>
    ///   This is the size of the file not the raw encoded contents
    ///   of the block.
    /// </value>
    [DataMember]
    public long Size
    {
        get
        {
            if (!_size.HasValue) GetInfo();
            return _size.Value;
        }
        set => _size = value;
    }

    /// <summary>
    ///   Determines if the link is a directory (folder).
    /// </summary>
    /// <value>
    ///   <b>true</b> if the link is a directory; Otherwise <b>false</b>,
    ///   the link is some type of a file.
    /// </value>
    [DataMember]
    public bool IsDirectory
    {
        get
        {
            if (!_isDirectory.HasValue) GetInfo();
            return _isDirectory.Value;
        }
        set => _isDirectory = value;
    }

    /// <summary>
    ///   The file name of the IPFS node.
    /// </summary>
    [DataMember]
    public string Name { get; set; }

    /// <inheritdoc />
    public IFileSystemLink ToLink(string name = "")
    {
        var link = new FileSystemLink
        {
            Name = string.IsNullOrWhiteSpace(name) ? Name : name,
            Id = Id,
            Size = Size,
        };
        return link;
    }

    /// <summary>
    ///   The client to IPFS.
    /// </summary>
    /// <value>
    ///   Used to fetch additional information on the node.
    /// </value>
    public IpfsClient IpfsClient
    {
        get
        {
            if (_ipfsClient == null)
            {
                lock (this)
                {
                    _ipfsClient = new();
                }
            }
            return _ipfsClient;
        }
        set => _ipfsClient = value;
    }

    private void GetInfo()
    {
        var node = IpfsClient.FileSystem.ListFileAsync(Id).Result;
        IsDirectory = node.IsDirectory;
        Links = node.Links;
        Size = node.Size;
    }

}