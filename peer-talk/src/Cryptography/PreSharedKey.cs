using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ipfs;

namespace PeerTalk.Cryptography
{
    /// <summary>
    ///   A secret (symmetric) key shared among some entities.
    /// </summary>
    /// <remarks>
    ///   This is specifically used for nodes in a private network.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
    public class PreSharedKey
    {
        const string codecName = "/key/swarm/psk/1.0.0/";

        /// <summary>
        ///   The key value.
        /// </summary>
        /// <value>
        ///   The value as a byte array.
        /// </value>
        public byte[] Value { get; set; }

        /// <summary>
        ///   The length of the key's value.
        /// </summary>
        /// <value>
        ///   The length in bits.
        /// </value>
        public int Length { get { return Value?.Length * 8 ?? 0; } }

        /// <summary>
        ///   Generate a new value of the specified length.
        /// </summary>
        /// <param name="length">
        ///   The length in bits of the new key value, defaults to 256.
        /// </param>
        /// <returns>
        ///   <b>this</b> for a fluent design.
        /// </returns>
        public PreSharedKey Generate(int length = 256)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                Value = new byte[length / 8];
                rng.GetBytes(Value);
            }
            return this;
        }

        /// <summary>
        ///   Write the key to text stream.
        /// </summary>
        /// <param name="text">
        ///   A text writer.
        /// </param>
        /// <param name="format">
        ///   Determines how the key <see cref="Value"/> is formatted.  Can be
        ///   "base16" or "base64".  Defaults to "base16".
        /// </param>
        /// <remarks>
        ///   The key is writen as three lines 
        ///   (1) the codec name "/key/swarm/psk/1.0.0/"
        ///   (2) the base encoding  "/base16/" or "/base64/", 
        ///   (3) the key value in the base encoding
        /// </remarks>
        public void Export(TextWriter text, string format = "base16")
        {
            text.WriteLine(codecName);
            text.Write("/");
            text.Write(format);
            text.WriteLine("/");
            switch (format)
            {
                case "base16":
                    text.WriteLine(Value.ToHexString());
                    break;
                case "base64":
                    text.WriteLine(Value.ToBase64NoPad());
                    break;
                default:
                    throw new Exception($"Unknown encoding '{format}'.");
            }
        }

        /// <summary>
        ///   Read the key from the text stream.
        /// </summary>
        /// <param name="text">
        ///   A text reader.
        /// </param>
        public void Import(TextReader text)
        {
            if (text.ReadLine() != codecName)
                throw new FormatException($"Expected '{codecName}'.");
            switch (text.ReadLine())
            {
                case "/base16/":
                    Value = text.ReadLine().ToHexBuffer();
                    break;
                case "/base64/":
                    Value = text.ReadLine().FromBase64NoPad();
                    break;
                default:
                    throw new FormatException("Unknown base encoding.");
            }
        }
    }
}
