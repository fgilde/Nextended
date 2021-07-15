using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Hashing
{
    public static class HashExtensions
    {
        public static HashingActions Hash(this string str, string salt = null)
        {
            return new HashingActions(str, salt);
        }
    }


    public class HashingActions
    {
        internal HashingActions(string str, string salt = null)
        {
            MD5 = () => StringHasher.MD5.Hash(str, salt);
            Sha265 = () => StringHasher.Sha265.Hash(str, salt);
            Sha512 = () => StringHasher.Sha512.Hash(str, salt);
        }

        public Func<string> MD5 { get; }
        public Func<string> Sha265 { get; }
        public Func<string> Sha512 { get; }
    }

}