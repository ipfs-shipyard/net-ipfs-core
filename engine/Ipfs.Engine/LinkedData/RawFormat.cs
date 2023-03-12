using PeterO.Cbor;

namespace Ipfs.Engine.LinkedData;

/// <summary>
///     Unlinked data.
/// </summary>
public class RawFormat : ILinkedDataFormat
{
    /// <inheritdoc />
    public CBORObject Deserialise(byte[] data)
    {
        return CBORObject.NewMap()
            .Add("data", data)
            .Add("links", null);
    }

    /// <inheritdoc />
    public byte[] Serialize(CBORObject data)
    {
        return data["data"].GetByteString();
    }
}