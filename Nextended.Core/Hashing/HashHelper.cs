using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Nextended.Core.Extensions;

namespace Nextended.Core.Hashing
{
    public static class HashHelper
    {
        private static string SaltIt(string text, string salt)
        {
            return salt + text;
        }

        public static string MD5(string text, string salt = null)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            salt ??= string.Empty;
            var hash = new MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(SaltIt(text, salt)));
            if (string.IsNullOrEmpty(salt))
                return BitConverter.ToString(hash);
            var stringBuilder = new StringBuilder();
            foreach (var t in hash)
                stringBuilder.Append(t.ToString("x2"));

            return stringBuilder.ToString().ToUpper();
        }

        public static string MD5FileHash(string fileName)
        {
            using var stream = File.OpenRead(fileName);
            return MD5(stream);
        }


        public static string MD5(Stream stream)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var originalPos = stream.Position;
            byte[] computeHash;
            try
            {
                stream.Position = 0;
                computeHash = md5.ComputeHash(stream);
            }
            finally
            {
                stream.Position = originalPos;
            }
            return BitConverter.ToString(computeHash).Replace("-", string.Empty);
        }
        
        public static string MD5(byte[] input)
        {
            return MD5(new MemoryStream(input));
        }

        public static string Sha256(string input, string salt = null)
        {
            if (input.IsNullOrEmpty()) return string.Empty;
            salt ??= string.Empty;
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(SaltIt(input, salt));
            var hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Creates a SHA256 hash of the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A hash.</returns>
        public static byte[] Sha256(byte[] input)
        {
            if (input == null) return null;
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }

        /// <summary>
        /// Creates a SHA512 hash of the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A hash</returns>
        public static string Sha512(string input, string salt = null)
        {
            if (input.IsNullOrEmpty()) return input;
            salt ??= string.Empty;
            using var sha = SHA512.Create();
            var bytes = Encoding.UTF8.GetBytes(SaltIt(input, salt));
            var hash = sha.ComputeHash(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}