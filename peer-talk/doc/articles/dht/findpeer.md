# Finding a peer

If a peer's ID is known but not it's addresses, then [Dht.FindPeerAsync](xref:PeerTalk.Routing.Dht1.FindPeerAsync*) can be used.
It will ask other peers for the details of the unknown peer.

```csharp
var peerId = new MultiHash("QmdpwjdB94eNm2Lcvp9JqoCxswo3AKQqjLuNZyLixmCxxx");
peer = await dht.FindPeerAsync(peerId);
```

## Algorithm

A [DHT distributed query](xref:PeerTalk.Routing.DistributedQuery`1) is created that iteratively asks the closest 
peers to the unknown peer for its details.