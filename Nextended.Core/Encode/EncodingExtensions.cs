using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Encode
{
    /// <summary>
    /// Provides extension methods for encoding and decoding strings.
    /// </summary>
    public static class EncodingExtensions
    {
        /// <summary>
        /// Creates an encoding actions object that provides access to various encoding/decoding operations for the string.
        /// </summary>
        /// <param name="str">The string to encode or decode.</param>
        /// <returns>An <see cref="EncodingActions"/> object with encoding/decoding options.</returns>
        public static EncodingActions EncodeDecode(this string str)
        {
            return new EncodingActions(str);
        }
    }

    /// <summary>
    /// Provides access to different encoding actions (Base64, Hex) for a string.
    /// </summary>
    public class EncodingActions
    {
        internal EncodingActions(string str)
        {
            
            Base64 = new EncodeAction(new Base64Encoding(), str);
            Hex = new EncodeAction(new HexEncoding(), str);
        }

        /// <summary>
        /// Gets the Base64 encoding action for the string.
        /// </summary>
        public EncodeAction Base64 { get; }
        
        /// <summary>
        /// Gets the Hex encoding action for the string.
        /// </summary>
        public EncodeAction Hex { get; }
    }

    /// <summary>
    /// Represents an encoding action that can encode or decode a string using a specific encoding algorithm.
    /// </summary>
    public class EncodeAction
    {
        private readonly IStringEncoding stringEncoding;
        private readonly string str;

        internal EncodeAction(IStringEncoding stringEncoding, string str)
        {
            this.stringEncoding = stringEncoding;
            this.str = str;
        }

        /// <summary>
        /// Encodes the string using the specified encoding algorithm.
        /// </summary>
        /// <returns>The encoded string.</returns>
        public string Encode()
        {
            return stringEncoding.Encode(str);
        }

        /// <summary>
        /// Decodes the string using the specified encoding algorithm.
        /// </summary>
        /// <returns>The decoded string.</returns>
        public string Decode()
        {
            return stringEncoding.Decode(str);
        }
    }

}