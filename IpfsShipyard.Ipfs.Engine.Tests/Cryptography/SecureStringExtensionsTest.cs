using System.Security;
using IpfsShipyard.Ipfs.Engine.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IpfsShipyard.Ipfs.Engine.Tests.Cryptography;

[TestClass]
public class SecureStringExtensionsTest
{
    [TestMethod]
    public void UseBytes()
    {
        var secret = new SecureString();
        var expected = new[] { 'a', 'b', 'c' };
        foreach (var c in expected)
        {
            secret.AppendChar(c);
        }

        secret.UseSecretBytes(bytes =>
        {
            Assert.AreEqual(expected.Length, bytes.Length);
            for (var i = 0; i < expected.Length; ++i)
            {
                Assert.AreEqual(expected[i], (int)bytes[i]);
            }
        });
    }
}