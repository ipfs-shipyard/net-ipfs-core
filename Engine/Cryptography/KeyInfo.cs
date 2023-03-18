using IpfsShipyard.Ipfs.Core;

namespace IpfsShipyard.Ipfs.Engine.Cryptography;

internal class KeyInfo : IKey
{
    public string Name { get; set; }
    public MultiHash Id { get; set; }
}