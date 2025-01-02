using System;

using System.Runtime.InteropServices;

namespace Nextended.Core.Extensions
{
    public static class Guider
    {
        private const char EqualsChar = '=';
        private const char Hyphen = '-';
        private const char Underscore = '_';
        private const char Slash = '/';
        private const byte SlashByte = (byte)Slash;
        private const char Plus = '+';
        private const byte PlusByte = (byte)Plus;

        public static string ToFormattedId(this Guid id)
        {            
#if !NETSTANDARD2_0
            Span<byte> idBytes = stackalloc byte[16];
            Span<byte> base64Bytes = stackalloc byte[24];
            MemoryMarshal.TryWrite(idBytes, ref id);
            System.Buffers.Text.Base64.EncodeToUtf8(idBytes, base64Bytes, out _, out _);

            Span<char> finalChars = stackalloc char[22];
            for (int i = 0; i < 22; i++)
            {
                finalChars[i] = base64Bytes[i] switch
                {
                    SlashByte => Hyphen,
                    PlusByte => Underscore,
                    _ => (char)base64Bytes[i]
                };
            }

            return new string(finalChars);
#else
           return Convert.ToBase64String(id.ToByteArray()).TrimEnd(EqualsChar).Replace(Slash, Hyphen).Replace(Plus, Underscore);
#endif
        }

#if !NETSTANDARD2_0
        public static Guid FromFormattedId(ReadOnlySpan<char> id)
        {
            Span<char> base64Chars = stackalloc char[24];
            for (int i = 0; i < 22; i++)
            {
                base64Chars[i] = id[i] switch
                {
                    Hyphen => Slash,
                    Underscore => Plus,
                    _ => id[i]
                };
            }

            base64Chars[22] = EqualsChar;
            base64Chars[23] = EqualsChar;

            Span<byte> idBytes = stackalloc byte[16];
            Convert.TryFromBase64Chars(base64Chars, idBytes, out _);
            return new Guid(idBytes);
        }
#endif

        public static int ToInt(this Guid value)
        {
            return BitConverter.ToInt32(value.ToByteArray(), 0);
        }

        public static long ToInt64(this Guid value)
        {
            return BitConverter.ToInt64(value.ToByteArray(), 0);
        }
    }
}