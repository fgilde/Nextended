#if !NETSTANDARD2_0
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nextended.Core;

public static class DeterministicGuid
{
    // FNV-1a constants — see http://www.isthe.com/chongo/tech/comp/fnv/
    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public static Guid Create([CallerMemberName] string key = "")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Seed Guid key must be non-empty.", nameof(key));

        var hi = Fnv1a64("v1::" + key);
        var lo = Fnv1a64("v2::" + key);

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(bytes[..8], hi);
        BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], lo);
        return new Guid(bytes);
    }

    private static ulong Fnv1a64(string s)
    {
        // We hash UTF-8 bytes (not chars) so the result matches across
        // platforms even if the .NET runtime decides to change char
        // representation in some future version.
        var bytes = Encoding.UTF8.GetBytes(s);
        var hash = FnvOffset;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= FnvPrime;
        }
        return hash;
    }
}
#endif