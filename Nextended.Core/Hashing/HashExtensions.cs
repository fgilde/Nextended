using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Hashing
{
    /// <summary>
    /// Provides extension methods for hashing strings using various algorithms.
    /// </summary>
    public static class HashExtensions
    {
        /// <summary>
        /// Creates a hashing actions object that provides access to various hashing operations for the string.
        /// </summary>
        /// <param name="str">The string to hash.</param>
        /// <param name="salt">Optional salt value to add to the hash for additional security.</param>
        /// <returns>A <see cref="HashingActions"/> object with hashing options.</returns>
        public static HashingActions Hash(this string str, string salt = null)
        {
            return new HashingActions(str, salt);
        }
    }


    /// <summary>
    /// Provides access to different hashing algorithms (MD5, SHA-256, SHA-512) for a string.
    /// </summary>
    public class HashingActions
    {
        internal HashingActions(string str, string salt = null)
        {
            MD5 = () => StringHasher.MD5.Hash(str, salt);
            Sha265 = () => StringHasher.Sha265.Hash(str, salt);
            Sha512 = () => StringHasher.Sha512.Hash(str, salt);
        }

        /// <summary>
        /// Gets a function that computes the MD5 hash of the string.
        /// </summary>
        public Func<string> MD5 { get; }
        
        /// <summary>
        /// Gets a function that computes the SHA-256 hash of the string.
        /// </summary>
        public Func<string> Sha265 { get; }
        
        /// <summary>
        /// Gets a function that computes the SHA-512 hash of the string.
        /// </summary>
        public Func<string> Sha512 { get; }
    }

}