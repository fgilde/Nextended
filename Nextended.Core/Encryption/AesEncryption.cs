﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Nextended.Core.Contracts;

namespace Nextended.Core.Encryption
{
#if NETSTANDARD2_0
    public class AesEncryption : IStringEncryption
    {
        // Standard-Salt (Beispiel)
        private static readonly byte[] DefaultSalt =
        {
            0x19, 0x35, 0x11, 0x3e, 0x2a, 0x4d, 0x65,
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76
        };

        /// <summary>
        /// Salt, kann bei Bedarf überschrieben werden.
        /// </summary>
        public byte[] Salt { get; set; } = DefaultSalt;

        /// <summary>
        /// Anzahl Iterationen für PBKDF2. 
        /// </summary>
        public int Iterations { get; set; } = 1223;

        public string Encrypt(string clearText, string key)
        {
            if (string.IsNullOrEmpty(clearText))
                throw new ArgumentException("clearText darf nicht leer sein.", nameof(clearText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key darf nicht leer sein.", nameof(key));

            // Klartext in Bytes konvertieren
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);

            using (var aes = Aes.Create())
            {
                // Für .NET Standard 2.0 gibt es diese Überladung leider nur mit HMAC-SHA1:
                var pdb = new Rfc2898DeriveBytes(key, Salt, Iterations * key.Length);

                aes.Key = pdb.GetBytes(32); // 256-Bit-Key
                aes.IV = pdb.GetBytes(16); // 128-Bit-IV

                using (var ms = new MemoryStream())
                {
                    // Verschlüsseln
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                    }

                    // Ergebnis als Base64-String zurückgeben
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("cipherText darf nicht leer sein.", nameof(cipherText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key darf nicht leer sein.", nameof(key));

            // Falls Blank in "+"
            cipherText = cipherText.Replace(" ", "+");

            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(key, Salt, Iterations * key.Length);

                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);

                using (var ms = new MemoryStream())
                {
                    // Entschlüsseln
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                    }

                    // Entschlüsselte Bytes wieder in Unicode-String zurückverwandeln
                    return Encoding.Unicode.GetString(ms.ToArray());
                }
            }
        }
    }
#else
    public class AesEncryption: IStringEncryption
    {
        private byte[] salt = { 0x19, 0x35, 0x11, 0x3e, 0x2a, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };
        public int Iterations { get; set; } = 1223;

        public string Encrypt(string clearText, string key)
        {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, salt, Iterations * key.Length, HashAlgorithmName.SHA512);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public string Decrypt(string cipherText, string key)
        {
            string EncryptionKey = "abc123";
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(key, salt, Iterations * key.Length, HashAlgorithmName.SHA512);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
#endif
}