using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.Ipfs.Http;

/// <summary>
///   The IPFS <see href="https://github.com/ipfs/specs/tree/master/merkledag">MerkleDag</see> is the data structure at the heart of IPFS. It is an acyclic directed graph whose edges are hashes.
/// </summary>
/// <remarks>
///   Initially an <b>MerkleNode</b> is just constructed with its Cid.
/// </remarks>
[DataContract]
public class MerkleNode : IMerkleNode<IMerkleLink>, IEquatable<MerkleNode>
{
    private bool _hasBlockStats;
    private long _blockSize;
    private string _name;
    private IEnumerable<IMerkleLink> _links;
    private IpfsClient _ipfsClient;

    /// <summary>
    ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
    ///   <see cref="Cid"/> and optional <see cref="Name"/>.
    /// </summary>
    /// <param name="id">
    ///   The <see cref="Cid"/> of the node.
    /// </param>
    /// <param name="name">A name for the node.</param>
    public MerkleNode(Cid id, string name = null)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        Id = id;
        Name = name;
    }

    /// <summary>
    ///   Creates a new instance of the <see cref="MerkleNode"/> with the specified
    ///   <see cref="Id">cid</see> and optional <see cref="Name"/>.
    /// </summary>
    /// <param name="path">
    ///   The string representation of a <see cref="Cid"/> of the node or "/ipfs/cid".
    /// </param>
    /// <param name="name">A name for the node.</param>
    public MerkleNode(string path, string name = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));

        if (path.StartsWith("/ipfs/"))
            path = path[6..];

        Id = Cid.Decode(path);
        Name = name;
    }

    /// <summary>
    ///   Creates a new instance of the <see cref="MerkleNode"/> from the
    ///   <see cref="IMerkleLink"/>.
    /// </summary>
    /// <param name="link">The link to a node.</param>
    public MerkleNode(IMerkleLink link)
    {
        Id = link.Id;
        Name = link.Name;
        _blockSize = link.Size;
        _hasBlockStats = true;
    }

    internal IpfsClient IpfsClient
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

    /// <inheritdoc />
    [DataMember]
    public Cid Id { get; }

    /// <summary>
    ///   The name for the node.  If unknown it is "" (not null).
    /// </summary>
    [DataMember]
    public string Name
    {
        get => _name;
        set => _name = value ?? string.Empty;
    }

    /// <summary>
    ///   Size of the raw, encoded node.
    /// </summary>
    [DataMember]
    public long BlockSize
    {
        get
        {
            GetBlockStats();
            return _blockSize;
        }
    }

    /// <inheritdoc />
    /// <seealso cref="BlockSize"/>
    [DataMember]
    public long Size => BlockSize;


    /// <inheritdoc />
    [DataMember]
    public IEnumerable<IMerkleLink> Links => _links ??= IpfsClient.Object.LinksAsync(Id).Result;

    /// <inheritdoc />
    [DataMember]
    public byte[] DataBytes => IpfsClient.Block.GetAsync(Id).Result.DataBytes;

    /// <inheritdoc />
    public Stream DataStream => IpfsClient.Block.GetAsync(Id).Result.DataStream;

    /// <inheritdoc />
    public IMerkleLink ToLink(string name = null)
    {
        return new DagLink(name ?? Name, Id, BlockSize);
    }

    /// <summary>
    ///   Get block statistics about the node, <c>ipfs block stat <i>key</i></c>
    /// </summary>
    /// <remarks>
    ///   The object stats include the block stats.
    /// </remarks>
    private void GetBlockStats()
    {
        if (_hasBlockStats)
            return;

        var stats = IpfsClient.Block.StatAsync(Id).Result;
        _blockSize = stats.Size;

        _hasBlockStats = true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        var that = obj as MerkleNode;
        return that != null && Id == that.Id;
    }

    /// <inheritdoc />
    public bool Equals(MerkleNode that)
    {
        return that != null && Id == that.Id;
    }

    /// <summary>
    ///  TODO
    /// </summary>
    public static bool operator ==(MerkleNode a, MerkleNode b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (ReferenceEquals(a, null)) return false;
        if (ReferenceEquals(b, null)) return false;

        return a.Equals(b);
    }

    /// <summary>
    ///  TODO
    /// </summary>
    public static bool operator !=(MerkleNode a, MerkleNode b)
    {
        if (ReferenceEquals(a, b)) return false;
        if (ReferenceEquals(a, null)) return true;
        if (ReferenceEquals(b, null)) return true;

        return !a.Equals(b);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return "/ipfs/" + Id;
    }

    /// <summary>
    ///  TODO
    /// </summary>
    public static implicit operator MerkleNode(string hash)
    {
        return new(hash);
    }

}