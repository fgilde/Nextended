using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nextended.Core.Contracts;

public class RijndaelEncryption : IStringEncryption
{
    private const int BlockSizeBits = 128;
    private const int KeySizeBits = 128;

    public int Iterations { get; set; } = 1347;

    public string Encrypt(string str, string key)
    {
        var salt = GenerateRandom(BlockSizeBits / 8); // 16 B
        var iv = GenerateRandom(BlockSizeBits / 8); // 16 B
        var plain = Encoding.UTF8.GetBytes(str);

        using var kdf = new Rfc2898DeriveBytes(key, salt, Iterations * key.Length);
        var keyBytes = kdf.GetBytes(KeySizeBits / 8); // 16 B

        using var rij = new RijndaelManaged
        {
            BlockSize = BlockSizeBits,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.PKCS7
        };

        using var encryptor = rij.CreateEncryptor(keyBytes, iv);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(plain, 0, plain.Length);
            cs.FlushFinalBlock();
        }

        // salt || iv || ciphertext
        var cipher = salt.Concat(iv).Concat(ms.ToArray()).ToArray();
        return Convert.ToBase64String(cipher);
    }

#if !NETSTANDARD
    public string Decrypt(string str, string key)
    {
        var all = Convert.FromBase64String(str);
        var ofs = 0;

        var salt = all.AsSpan(ofs, KeySizeBits / 8).ToArray(); ofs += KeySizeBits / 8;
        var iv = all.AsSpan(ofs, KeySizeBits / 8).ToArray(); ofs += KeySizeBits / 8;
        var ct = all.AsSpan(ofs).ToArray();

        using var kdf = new Rfc2898DeriveBytes(key, salt, Iterations * key.Length);
        var keyBytes = kdf.GetBytes(KeySizeBits / 8);

        using var rij = new RijndaelManaged
        {
            BlockSize = BlockSizeBits,
            Mode = CipherMode.CBC,
            Padding = PaddingMode.PKCS7
        };

        using var decryptor = rij.CreateDecryptor(keyBytes, iv);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
        {
            cs.Write(ct, 0, ct.Length);
            cs.FlushFinalBlock();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }
#else
    public string Decrypt(string str, string key)
    {
        var all = Convert.FromBase64String(str);

        const int segment = KeySizeBits / 8; // 16 Bytes bei 128 Bit
        var salt = new byte[segment];
        var iv   = new byte[segment];

        Array.Copy(all, 0,           salt, 0, segment);
        Array.Copy(all, segment,     iv,   0, segment);
        var cipherLen = all.Length - (segment * 2);
        var ct = new byte[cipherLen];
        Array.Copy(all, segment * 2, ct,   0, cipherLen);

        var keyBytes = new byte[KeySizeBits / 8];
        using (var password = new Rfc2898DeriveBytes(key, salt, Iterations * key.Length))
        {
            keyBytes = password.GetBytes(KeySizeBits / 8);
        }

        using (var rij = new RijndaelManaged())
        {
            rij.BlockSize = BlockSizeBits;
            rij.Mode = CipherMode.CBC;
            rij.Padding = PaddingMode.PKCS7;

            using (var decryptor = rij.CreateDecryptor(keyBytes, iv))
            using (var ms = new MemoryStream())
            {
                // Write-Modus entschlüsseln (robust)
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    cs.Write(ct, 0, ct.Length);
                    cs.FlushFinalBlock();
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }

#endif

    private static byte[] GenerateRandom(int size)
    {
        var buf = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(buf);
        return buf;
    }
}
