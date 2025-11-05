using System;

namespace Nextended.Core.Contracts
{
    /// <summary>
    /// Defines the contract for string encoding and decoding operations.
    /// </summary>
    public interface IStringEncoding
    {
        /// <summary>
        /// Encodes a string using the specific encoding algorithm.
        /// </summary>
        /// <param name="str">The string to encode.</param>
        /// <returns>The encoded string.</returns>
        string Encode(string str);
        
        /// <summary>
        /// Decodes a string using the specific encoding algorithm.
        /// </summary>
        /// <param name="str">The encoded string to decode.</param>
        /// <returns>The decoded string.</returns>
        string Decode(string str);
    }

    /// <summary>
    /// Extends <see cref="IStringEncoding"/> with hooks for pre- and post-processing during encoding and decoding operations.
    /// </summary>
    public interface IStringEncodingExt : IStringEncoding
    {
        /// <summary>
        /// Sets a function to execute before encoding.
        /// </summary>
        /// <param name="onBeforeEncode">The function to execute before encoding.</param>
        /// <returns>The current instance for method chaining.</returns>
        IStringEncodingExt BeforeEncode(Func<string, string> onBeforeEncode);
        
        /// <summary>
        /// Sets a function to execute after encoding.
        /// </summary>
        /// <param name="onAfterEncode">The function to execute after encoding.</param>
        /// <returns>The current instance for method chaining.</returns>
        IStringEncodingExt AfterEncode(Func<string, string> onAfterEncode);
        
        /// <summary>
        /// Sets a function to execute before decoding.
        /// </summary>
        /// <param name="onBeforeDecode">The function to execute before decoding.</param>
        /// <returns>The current instance for method chaining.</returns>
        IStringEncodingExt BeforeDecode(Func<string, string> onBeforeDecode);
        
        /// <summary>
        /// Sets a function to execute after decoding.
        /// </summary>
        /// <param name="onAfterDecode">The function to execute after decoding.</param>
        /// <returns>The current instance for method chaining.</returns>
        IStringEncodingExt AfterDecode(Func<string, string> onAfterDecode);
    }

}