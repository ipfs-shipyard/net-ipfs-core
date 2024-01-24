using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Ipfs
{
    /// <summary>
    ///   A codec for Base-36.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Provides encoding and decoding functionality for Base-36, with methods <see cref="EncodeToStringUc"/> and <see cref="EncodeToStringLc"/> for encoding, 
    ///   and <see cref="DecodeString"/> for decoding. The encoding methods offer both uppercase and lowercase options.
    ///   </para>
    ///   <para>
    ///   The implementation is case-insensitive for decoding and allows for efficient conversion between byte arrays and Base-36 strings.
    ///   </para>
    ///   <para>
    ///   Ported from https://github.com/multiformats/go-base36/blob/v0.2.0/base36.go
    ///   </para>
    /// </remarks>
    public static class Base36
    {
        // Constants for the encoding alphabets
        private const string UcAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LcAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        private const int MaxDigitOrdinal = 'z';
        private const byte MaxDigitValueB36 = 35;

        // Reverse lookup table for decoding
        private static readonly byte[] RevAlphabet = new byte[MaxDigitOrdinal + 1];

        // Static constructor to initialize the reverse lookup table
        static Base36()
        {
            // Initialize the reverse alphabet array with default values
            for (int i = 0; i < RevAlphabet.Length; i++)
            {
                RevAlphabet[i] = MaxDigitValueB36 + 1;
            }

            // Populate the reverse alphabet array for decoding
            for (int i = 0; i < UcAlphabet.Length; i++)
            {
                char c = UcAlphabet[i];
                RevAlphabet[c] = (byte)i;
                if (c > '9')
                {
                    RevAlphabet[char.ToLower(c)] = (byte)i;
                }
            }
        }

        /// <summary>
        ///   Encodes a byte array to a Base-36 string using uppercase characters.
        /// </summary>
        /// <param name="bytes">
        ///   The byte array to encode.
        /// </param>
        /// <returns>
        ///   The encoded Base-36 string in uppercase.
        /// </returns>
        public static string EncodeToStringUc(byte[] bytes) => Encode(bytes, UcAlphabet);

        /// <summary>
        ///   Encodes a byte array to a Base-36 string using lowercase characters.
        /// </summary>
        /// <param name="bytes">
        ///   The byte array to encode.
        /// </param>
        /// <returns>
        ///   The encoded Base-36 string in lowercase.
        /// </returns>
        public static string EncodeToStringLc(byte[] bytes) => Encode(bytes, LcAlphabet);

        // Core encoding logic for Base-36 conversion
        private static string Encode(byte[] input, string alphabet)
        {
            int zeroCount = 0;
            while (zeroCount < input.Length && input[zeroCount] == 0)
            {
                zeroCount++;
            }

            int size = zeroCount + (input.Length - zeroCount) * 277 / 179 + 1;
            byte[] buffer = new byte[size];
            int index, stopIndex;
            uint carry;

            stopIndex = size - 1;
            for (int i = zeroCount; i < input.Length; i++)
            {
                index = size - 1;
                carry = input[i];
                while (index > stopIndex || carry != 0)
                {
                    carry += (uint)(buffer[index]) * 256;
                    buffer[index] = (byte)(carry % 36);
                    carry /= 36;
                    index--;
                }
                stopIndex = index;
            }

            // The purpose of this loop is to skip over the portion of the buffer that contains only zeros (after accounting for any leading zeros in the original input).
            // This is important for the encoding process, as these leading zeros are not represented in the base-36 encoded string.
            for (stopIndex = zeroCount; stopIndex < size && buffer[stopIndex] == 0; stopIndex++)
            {
            }

            // Once the first non-zero byte is found, the actual encoding of the non-zero part of the buffer can begin.
            byte[] valueBuffer = new byte[buffer.Length - (stopIndex - zeroCount)];
            for (int i = 0; i < valueBuffer.Length; i++)
            {
                valueBuffer[i] = (byte)alphabet[buffer[stopIndex - zeroCount + i]];
            }

            return Encoding.ASCII.GetString(valueBuffer);
        }

        /// <summary>
        ///   Decodes a Base-36 encoded string to a byte array.
        /// </summary>
        /// <param name="s">
        ///   The Base-36 encoded string to decode.
        /// </param>
        /// <returns>
        ///   The decoded byte array.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   Thrown if the input string is null or empty.
        /// </exception>
        /// <exception cref="FormatException">
        ///   Thrown if the input string contains characters not valid in Base-36.
        /// </exception>
        public static byte[] DecodeString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Cannot decode a zero-length string.");
            }

            int zeroCount = 0;
            while (zeroCount < s.Length && s[zeroCount] == '0')
            {
                zeroCount++;
            }

            byte[] binu = new byte[2 * ((s.Length) * 179 / 277 + 1)];
            uint[] outi = new uint[(s.Length + 3) / 4];

            foreach (char r in s)
            {
                if (r > MaxDigitOrdinal || RevAlphabet[r] > MaxDigitValueB36)
                {
                    throw new FormatException($"Invalid base36 character ({r}).");
                }

                ulong c = RevAlphabet[r];

                for (int j = outi.Length - 1; j >= 0; j--)
                {
                    ulong t = (ulong)outi[j] * 36 + c;
                    c = (t >> 32);
                    outi[j] = (uint)(t & 0xFFFFFFFF);
                }
            }

            uint mask = (uint)((s.Length % 4) * 8);
            if (mask == 0)
            {
                mask = 32;
            }
            mask -= 8;

            int outIndex = 0;
            for (int j = 0; j < outi.Length; j++)
            {
                for (; mask < 32; mask -= 8)
                {
                    binu[outIndex] = (byte)(outi[j] >> (int)mask);
                    outIndex++;
                }
                mask = 24;
            }

            for (int msb = zeroCount; msb < outIndex; msb++)
            {
                if (binu[msb] > 0)
                {
                    int lengthToCopy = outIndex - msb;
                    byte[] result = new byte[lengthToCopy];
                    Array.Copy(binu, msb, result, 0, lengthToCopy);
                    return result;
                }
            }

            return new byte[outIndex - zeroCount];
        }
    }
}
