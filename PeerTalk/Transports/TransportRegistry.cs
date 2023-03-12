using System;
using System.Collections.Generic;

namespace IpfsShipyard.PeerTalk.Transports;

internal static class TransportRegistry
{
    public static Dictionary<string, Func<IPeerTransport>> Transports;

    static TransportRegistry()
    {
        Transports = new();
        Register("tcp", () => new Tcp());
        Register("udp", () => new Udp());
    }

    public static void Register(string protocolName, Func<IPeerTransport> transport)
    {
        Transports.Add(protocolName, transport);
    }

    public static void Deregister(string protocolName)
    {
        Transports.Remove(protocolName);
    }
}