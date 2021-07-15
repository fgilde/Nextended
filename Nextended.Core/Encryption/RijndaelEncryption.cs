using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nextended.Core.Contracts;

namespace Nextended.Core.Encryption
{
    public class RijndaelEncryption : IStringEncryption
    {
        private const int blockSize = 128;
        private const int Keysize = 128;

        // This constant determines the number of iterations for the password bytes generation function.
        public int Iterations { get; set; } = 1347;


        public string Encrypt(string str, string key)
        {
            var saltStringBytes = GenerateBitsOfRandomEntropy(blockSize);
            var ivStringBytes = GenerateBitsOfRandomEntropy(blockSize);
            var plainTextBytes = Encoding.UTF8.GetBytes(str);
            using (var password = new Rfc2898DeriveBytes(key, saltStringBytes, Iterations * key.Length))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = blockSize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public string Decrypt(string str, string key)
        {
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(str);
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

            using (var password = new Rfc2898DeriveBytes(key, saltStringBytes, Iterations * key.Length))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = blockSize;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream(cipherTextBytes))
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                var plainTextBytes = new byte[cipherTextBytes.Length];
                                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                memoryStream.Close();
                                cryptoStream.Close();
                                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                            }
                        }
                    }
                }
            }
        }

        private static byte[] GenerateBitsOfRandomEntropy(int size)
        {
            var randomBytes = new byte[size / 8]; // 32 Bytes will give us 256 bits.
            using var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(randomBytes);
            return randomBytes;
        }

    }
}