namespace Nextended.Core.Contracts
{
    /// <summary>
    /// Interface for string hashing operations with optional salt support.
    /// </summary>
    public interface IStringHashing
    {
        /// <summary>
        /// Computes a hash of the input string, optionally using a salt for additional security.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <param name="salt">Optional salt to add to the hash for additional security. If null, no salt is used.</param>
        /// <returns>The computed hash as a string.</returns>
        public string Hash(string input, string salt = null);
    }
}