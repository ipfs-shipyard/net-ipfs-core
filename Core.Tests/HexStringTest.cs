using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Core.Tests;

[TestClass]
public class HexStringTest
{
    [TestMethod]
    public void Encode()
    {
        var buffer = Enumerable.Range(byte.MinValue, byte.MaxValue).Select(b => (byte) b).ToArray();
        var lowerHex = string.Concat(buffer.Select(b => b.ToString("x2")).ToArray());
        var upperHex = string.Concat(buffer.Select(b => b.ToString("X2")).ToArray());

        Assert.AreEqual(lowerHex, buffer.ToHexString(), "encode default");
        Assert.AreEqual(lowerHex, buffer.ToHexString(), "encode general");
        Assert.AreEqual(lowerHex, buffer.ToHexString("x"), "encode lower");
        Assert.AreEqual(upperHex, buffer.ToHexString("X"), "encode upper");
    }

    [TestMethod]
    public void Decode()
    {
        var buffer = Enumerable.Range(byte.MinValue, byte.MaxValue).Select(b => (byte)b).ToArray();
        var lowerHex = string.Concat(buffer.Select(b => b.ToString("x2")).ToArray());
        var upperHex = string.Concat(buffer.Select(b => b.ToString("X2")).ToArray());

        CollectionAssert.AreEqual(buffer, lowerHex.ToHexBuffer(), "decode lower");
        CollectionAssert.AreEqual(buffer, upperHex.ToHexBuffer(), "decode upper");
    }

    [TestMethod]
    public void InvalidFormatSpecifier()
    {
        ExceptionAssert.Throws<FormatException>(() => HexString.Encode(Array.Empty<byte>(), "..."));
    }

    [TestMethod]
    public void InvalidHexStrings()
    {
        ExceptionAssert.Throws<InvalidDataException>(() => HexString.Decode("0"));
        ExceptionAssert.Throws<InvalidDataException>(() => HexString.Decode("0Z"));
    }
}