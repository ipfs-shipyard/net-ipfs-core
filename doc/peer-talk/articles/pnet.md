# Private Network

A private network is a group of [peers](peer.md) that share the same 256-bit 
[secret key](xref:PeerTalk.Cryptography.PreSharedKey).  All communication between the
peers is encryped with the [XSalsa20 cipher](https://en.wikipedia.org/wiki/Salsa20).  The
specification is at [PSK v1](https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md)
and is implemented by the [Psk1Protector](xref:PeerTalk.SecureCommunication.Psk1Protector) class.

The private network is defined by the symmetric secret key, which is known by all members. 
All traffic leaving the peer is encrypted and there is no characteristic 
handshake. The secret key is just a random number; public/private 
keys and certificates are not needed.

## Joining

The local peer becomes a member of the private network by having the
secret key and adding it to the [Swarm.NetworkProtector](xref:PeerTalk.Swarm.NetworkProtector)

```csharp
var key = new PreSharedKey { Value = "e8d6...".ToHexBuffer() };
var protector = new Psk1Protector { Key = key }
swarm.NetworkProtector = protector;
```
