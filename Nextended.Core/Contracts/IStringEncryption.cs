namespace Nextended.Core.Contracts
{
    /// <summary>
    /// Interface for string encryption and decryption operations.
    /// </summary>
    public interface IStringEncryption
    {
        /// <summary>
        /// Encrypts the specified string using the provided key.
        /// </summary>
        /// <param name="str">The string to encrypt.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>The encrypted string, typically Base64-encoded.</returns>
        string Encrypt(string str, string key);
        
        /// <summary>
        /// Decrypts the specified encrypted string using the provided key.
        /// </summary>
        /// <param name="str">The encrypted string to decrypt.</param>
        /// <param name="key">The decryption key.</param>
        /// <returns>The decrypted plain text string.</returns>
        string Decrypt(string str, string key);
    }
}