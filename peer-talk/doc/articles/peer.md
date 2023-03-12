# Peer

A [peer](xref:Ipfs.Peer) is a node in the IPFS network; the [IpfsEngine](https://github.com/richardschneider/net-ipfs-engine) is a C# implementation.
Among other properties, it has an [unique identifier](xref:Ipfs.Peer.Id) and [addresses](xref:Ipfs.Peer.Addresses) that it responds to.

## Sending a message

To talk to a peer, the [Swarm](xref:PeerTalk.Swarm.DialAsync*) is used to establish a 
connection and then select a [protocol](http://todo).

```csharp
using (var stream = await swarm.DialAsync(peer, "/ipfs/id/1.0.0"))
{
    // Send a message to the stream
}
```

## Receiving a message

To receive a message from a peer, a protocol handler is registered with the 
[Swarm](xref:PeerTalk.Swarm.AddProtocol*)

```csharp
TODO
```