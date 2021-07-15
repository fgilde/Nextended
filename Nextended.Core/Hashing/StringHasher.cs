using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Hashing
{
    public class StringHasher : IStringHashing
    {
        private readonly Func<string, string, string> hashFunc;

        internal StringHasher(Func<string, string, string> hashFunc)
        {
            this.hashFunc = hashFunc;
        }

        public string Hash(string input, string salt = null)
        {
            return hashFunc(input, salt);
        }

        public static IStringHashing Sha265 => new StringHasher(HashHelper.Sha256);

        public static IStringHashing Sha512 => new StringHasher(HashHelper.Sha512);
        
        public static IStringHashing MD5 => new StringHasher(HashHelper.MD5);
    }
}