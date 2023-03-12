using System;
using System.Collections.Generic;
using System.Text;
using IpfsShipyard.Ipfs.Core.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Core.Tests;

[TestClass]
public class MultiBaseTest
{
    [TestMethod]
    public void Codec()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var bytes1 = MultiBase.Decode(MultiBase.Encode(bytes));
        var bytes2 = MultiBase.Decode(MultiBase.Encode(bytes, "base16"));
        CollectionAssert.AreEqual(bytes, bytes1);
        CollectionAssert.AreEqual(bytes, bytes2);
    }

    [TestMethod]
    public void Encode_Unknown_Algorithm()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        ExceptionAssert.Throws<KeyNotFoundException>(() => MultiBase.Encode(bytes, "unknown"));
    }

    [TestMethod]
    public void Encode_Null_Data_Not_Allowed()
    {
        ExceptionAssert.Throws<ArgumentNullException>(() => MultiBase.Encode(null));
    }

    [TestMethod]
    public void Decode_Bad_Formats()
    {
        ExceptionAssert.Throws<ArgumentNullException>(() => MultiBase.Decode(null));
        ExceptionAssert.Throws<ArgumentNullException>(() => MultiBase.Decode(""));
        ExceptionAssert.Throws<ArgumentNullException>(() => MultiBase.Decode("   "));

        ExceptionAssert.Throws<FormatException>(() => MultiBase.Decode("?"));
        ExceptionAssert.Throws<FormatException>(() => MultiBase.Decode("??"));
        ExceptionAssert.Throws<FormatException>(() => MultiBase.Decode("???"));
        ExceptionAssert.Throws<FormatException>(() => MultiBase.Decode("fXX"));
    }

    private class TestVector
    {
        public string Algorithm { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
    }

    private readonly TestVector[] _testVectors = new TestVector[]
    {
        new()
        {
            Algorithm = "base16",
            Input = "yes mani !",
            Output = "f796573206d616e692021"
        },
        new()
        {
            Algorithm = "base32",
            Input = "yes mani !",
            Output = "bpfsxgidnmfxgsibb"
        },
        new()
        {
            Algorithm = "base32pad",
            Input = "yes mani !",
            Output = "cpfsxgidnmfxgsibb"
        },
        new()
        {
            Algorithm = "base32",
            Input = "f",
            Output = "bmy"
        },
        new()
        {
            Algorithm = "base32pad",
            Input = "f",
            Output = "cmy======"
        },
        new()
        {
            Algorithm = "base32hex",
            Input = "f",
            Output = "vco"
        },
        new()
        {
            Algorithm = "base32hexpad",
            Input = "f",
            Output = "tco======"
        },
        new()
        {
            Algorithm = "base64pad",
            Input = "f",
            Output = "MZg=="
        },
        new()
        {
            Algorithm = "base64",
            Input = "f",
            Output = "mZg"
        },
        new()
        {
            Algorithm = "base64",
            Input = "\u00f7\u00ef\u00ff",
            Output = "mw7fDr8O/"
        },
        new()
        {
            Algorithm = "base64url",
            Input = "\u00f7\u00ef\u00ff",
            Output = "uw7fDr8O_"
        },
        new()
        {
            Algorithm = "base64url",
            Input = "f",
            Output = "uZg"
        },
        new()
        {
            Algorithm = "base64url",
            Input = "fo",
            Output = "uZm8"
        },
        new()
        {
            Algorithm = "base64url",
            Input = "foo",
            Output = "uZm9v"
        },
        new()
        {
            Algorithm = "BASE16",
            Input = "yes mani !",
            Output = "F796573206D616E692021"
        },
        new()
        {
            Algorithm = "BASE32",
            Input = "yes mani !",
            Output = "BPFSXGIDNMFXGSIBB"
        },
        new()
        {
            Algorithm = "BASE32PAD",
            Input = "yes mani !",
            Output = "CPFSXGIDNMFXGSIBB"
        },
        new()
        {
            Algorithm = "BASE32",
            Input = "f",
            Output = "BMY"
        },
        new()
        {
            Algorithm = "BASE32PAD",
            Input = "f",
            Output = "CMY======"
        },
        new()
        {
            Algorithm = "BASE32HEX",
            Input = "f",
            Output = "VCO"
        },
        new()
        {
            Algorithm = "BASE32HEXPAD",
            Input = "f",
            Output = "TCO======"
        },
        new()
        {
            Algorithm = "base32z",
            Input="Decentralize everything!!",
            Output ="het1sg3mqqt3gn5djxj11y3msci3817depfzgqejb"
        },
        new()
        {
            Algorithm = "base32z",
            Input ="yes mani !",
            Output ="hxf1zgedpcfzg1ebb"
        },
        new()
        {
            Algorithm = "base32z",
            Input ="hello world",
            Output ="hpb1sa5dxrb5s6hucco"
        },
        new()
        {
            Algorithm = "base32z",
            Input ="\x00\x00yes mani !",
            Output ="hyyy813murbssn5ujryoo"
        },

    };

    /// <summary>
    ///   Test vectors from various sources.
    /// </summary>
    [TestMethod]
    public void CheckMultiBase()
    {
        foreach (var v in _testVectors)
        {
            var bytes = Encoding.UTF8.GetBytes(v.Input);
            var s = MultiBase.Encode(bytes, v.Algorithm);
            Assert.AreEqual(v.Output, s);
            CollectionAssert.AreEqual(bytes, MultiBase.Decode(s));
        }
    }

    [TestMethod]
    public void EmptyData()
    {
        var empty = new byte[0];
        foreach (var alg in MultiBaseAlgorithm.All)
        {
            var s = MultiBase.Encode(empty, alg.Name);
            CollectionAssert.AreEqual(empty, MultiBase.Decode(s), alg.Name);
        }
    }

    [TestMethod]
    public void Invalid_Encoded_String()
    {
        foreach (var alg in MultiBaseAlgorithm.All)
        {
            var bad = alg.Code + "?";
            ExceptionAssert.Throws<FormatException>(() => MultiBase.Decode(bad));
        }
    }

}