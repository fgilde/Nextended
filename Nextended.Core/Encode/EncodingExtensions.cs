using System;
using Nextended.Core.Contracts;

namespace Nextended.Core.Encode
{
    public static class EncodingExtensions
    {
        public static EncodingActions EncodeDecode(this string str)
        {
            return new EncodingActions(str);
        }
    }

    public class EncodingActions
    {
        internal EncodingActions(string str)
        {
            
            Base64 = new EncodeAction(new Base64Encoding(), str);
            Hex = new EncodeAction(new HexEncoding(), str);
        }

        public EncodeAction Base64 { get; }
        public EncodeAction Hex { get; }
    }

    public class EncodeAction
    {
        private readonly IStringEncoding stringEncoding;
        private readonly string str;

        internal EncodeAction(IStringEncoding stringEncoding, string str)
        {
            this.stringEncoding = stringEncoding;
            this.str = str;
        }

        public string Encode()
        {
            return stringEncoding.Encode(str);
        }

        public string Decode()
        {
            return stringEncoding.Decode(str);
        }
    }

}