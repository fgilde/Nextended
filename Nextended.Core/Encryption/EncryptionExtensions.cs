using Nextended.Core.Encode;
using Nextended.Core.Extensions;

namespace Nextended.Core.Encryption
{
    public static class EncryptionExtensions
    {
        public static string Encrypt(this string str, string key = null)
        {
            return new RijndaelEncryption().Encrypt(str.EncodeDecode().Base64.Encode(), key);
        }

        public static string Decrypt(this string str, string key = null)
        {
            return new RijndaelEncryption().Decrypt(str, key).EncodeDecode().Base64.Decode();
        }
    }
}