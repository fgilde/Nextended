using System;
using System.Text;

namespace Nextended.Core.Encode
{
    /// <summary>
    /// Provides hexadecimal encoding and decoding functionality for strings.
    /// Converts strings to their hexadecimal representation and vice versa.
    /// </summary>
    public class HexEncoding : StringEncodingBase<HexEncoding>
    {
        public HexEncoding()
        {
            ClearReplacements().AddReplacements("-", "");
        }

        protected override string EncodeCore(string str)
        {
            return BitConverter.ToString(Encoding.GetBytes(str));
        }

        protected override string DecodeCore(string str)
        {
            var bytes = new byte[str.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return Encoding.GetString(bytes);
        }
    }
}