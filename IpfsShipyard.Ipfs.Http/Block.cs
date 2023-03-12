using System.IO;
using System.Runtime.Serialization;
using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.Ipfs.Http;

/// <inheritdoc />
[DataContract]
public class Block : IDataBlock
{
    long? _size;

    /// <inheritdoc />
    [DataMember]
    public Cid Id { get; set; }

    /// <inheritdoc />
    [DataMember]
    public byte[] DataBytes { get; set; }

    /// <inheritdoc />
    public Stream DataStream
    {
        get
        {
            return new MemoryStream(DataBytes, false);
        }
    }

    /// <inheritdoc />
    [DataMember]
    public long Size
    {
        get
        {
            if (_size.HasValue)
            {
                return _size.Value;
            }
            return DataBytes.Length;
        }
        set
        {
            _size = value;
        }
    }

}