# net-ipfs-core 

![Version](https://img.shields.io/nuget/v/IpfsShipyard.Ipfs.Core.svg)

The core objects and interfaces of the [IPFS](https://github.com/ipfs/ipfs) (Inter Planetary File System) for .Net (C#, VB, F# etc.)

The interplanetary file system is the permanent web. It is a new hypermedia distribution protocol, addressed by content and identities. IPFS enables the creation of completely distributed applications. It aims to make the web faster, safer, and more open.

This library supports .NET Standard 2.0.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/IpfsShipyard.Ipfs.Core).  To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package IpfsShipyard.Ipfs.Core
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package IpfsShipyard.Ipfs.Core

## Major objects

- [MerkleDag](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.DagNode.html)
- [MultiAddress](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.MultiAddress.html)
- [MultiHash](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.MultiHash.html)

See the [API Documentation](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.html) for a list of all objects.

### MultiHash

All hashes in IPFS are encoded with [multihash](https://github.com/multiformats/multihash), a self-describing hash format. The actual hash function used depends on security requirements. The cryptosystem of IPFS is upgradeable, meaning that as hash functions are broken, networks can shift to stronger hashes. There is no free lunch, as objects may need to be rehashed, or links duplicated. But ensuring that tools built do not assume a pre-defined length of hash digest means tools that work with today's hash functions will also work with tomorrows longer hash functions too.

### MultiAddress

A standard way to represent a networks address that supports [multiple network protocols](https://github.com/multiformats/multiaddr). It is represented as a series of tuples, a protocol code and an optional value.  For example, an IPFS file at a sepcific address over ipv4 and tcp is 

    /ip4/10.1.10.10/tcp/80/ipfs/QmVcSqVEsvm5RR9mBLjwpb2XjFVn5bPdPL69mL8PH45pPC

### Merkle DAG

The [DagNode](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.DagNode.html) is a directed acyclic graph whose edges are a 
[DagLink](https://richardschneider.github.io/net-ipfs-core/api/Ipfs.DagLink.html). This means that links to objects can authenticate 
the objects themselves, and that every object contains a secure 
representation of its children.

Every Merkle is a directed acyclic graph (DAG) because each node is accessed via its name (the hash of `DagNode`). Each branch of Merkle is the hash of its local content (data and links);  naming children by their hash instead of their full contents. So after creation there is no way to edit a DagNode. This prevents cycles (assuming there are no hash collisions) since one can not link the first created node to the last note to create the last reference. 

## Related Projects

- [IPFS HTTP Client](https://github.com/ipfs-shipyard/net-ipfs-http-client) - A .Net client library for the IPFS HTTP API.

## License
The IPFS Core library is licensed under the [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form") license. Refer to the [LICENSE](LICENSE) file for more information.

