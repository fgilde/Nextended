using System;
using System.Text;

namespace Nextended.Core.Encode
{
    /// <summary>
    /// Provides Base64 encoding and decoding functionality for strings.
    /// </summary>
    public class Base64Encoding: StringEncodingBase<Base64Encoding>
    {
        protected override string EncodeCore(string str)
        {
            return Convert.ToBase64String(Encoding.GetBytes(str));
        }

        protected override string DecodeCore(string str)
        {
            return Encoding.GetString(Convert.FromBase64String(str));
        }
    }
}