using System.Text;
using IpfsShipyard.Ipfs.Core;
using IpfsShipyard.PeerTalk.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace IpfsShipyard.PeerTalk.Tests.Cryptography;

[TestClass]
public class CtrStreamCipherTest
{

    [TestMethod]
    public void SameAsNodeJs()
    {
        var key = new byte[32];
        var iv = new byte[16];
        var encrypt = new CtrStreamCipher(new AesEngine());
        var p = new ParametersWithIV(new KeyParameter(key), iv);
        encrypt.Init(true, p);

        var plain = new byte[] { (byte)'a' };
        var actual = new byte[plain.Length];

        var expected = new byte[] { 0xbd };
        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        Assert.AreEqual(expected[0], actual[0]);

        expected = new byte[] { 0xf4 };
        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        Assert.AreEqual(expected[0], actual[0]);
    }

    [TestMethod]
    public void MultiBlock()
    {
        var key = new byte[32];
        var iv = new byte[16];
        var encrypt = new CtrStreamCipher(new AesEngine());
        var p = new ParametersWithIV(new KeyParameter(key), iv);
        encrypt.Init(true, p);

        var plain = "this is some text that spans multiple blocks"u8.ToArray();
        var expected1 = "a8fda90b8229faa9de27cf71b2f045ff272ffe93a63116cad902da82e4a606e7bace305128400902682daea0".ToHexBuffer();
        var expected2 = "ce9bf46b520970ea44c94711f1d690f6012641e6bc3e915b3d2b8f0861852c483365469b2261a98deed81443".ToHexBuffer();
        var actual = new byte[plain.Length];

        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        CollectionAssert.AreEqual(expected1, actual);

        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        CollectionAssert.AreEqual(expected2, actual);
    }

    [TestMethod]
    public void SingleBlock()
    {
        var key = new byte[32];
        var iv = new byte[16];
        var encrypt = new CtrStreamCipher(new AesEngine());
        var p = new ParametersWithIV(new KeyParameter(key), iv);
        encrypt.Init(true, p);

        var plain = "1234567890123456"u8.ToArray();
        var expected1 = "eda7f34c9776beb194789326a1b015b1".ToHexBuffer();
        var expected2 = "623db9cff2730181905385c3f7ff46bd".ToHexBuffer();
        var actual = new byte[plain.Length];

        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        CollectionAssert.AreEqual(expected1, actual);

        encrypt.ProcessBytes(plain, 0, plain.Length, actual, 0);
        CollectionAssert.AreEqual(expected2, actual);
    }
}