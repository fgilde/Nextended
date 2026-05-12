using System;
using System.Security.Cryptography;
using System.Text;
using Nextended.ResponseFilters.Reflection;
using Nextended.ResponseFilters.Rules;

namespace Nextended.ResponseFilters.Builders;

/// <summary>
/// Builder that replaces a <see cref="string"/> property with a hash of its current value.
/// Default algorithm is SHA-256, emitted as lowercase hex.
/// </summary>
/// <remarks>
/// Use cases: producing stable pseudonyms for analytics responses, redacting tokens while keeping
/// equality comparable downstream. Null/empty inputs pass through unchanged.
/// </remarks>
public sealed class HashBuilder<T> : RuleBuilderBase<HashBuilder<T>, T> where T : class
{
    private readonly PropertyAccessor _accessor;
    private Func<string, string>? _customHasher;
    private HashAlgorithmName _algorithm = HashAlgorithmName.SHA256;

    internal HashBuilder(ResponseFilter<T> filter, PropertyAccessor accessor) : base(filter)
    {
        _accessor = accessor;
    }

    /// <summary>Use SHA-256 (default).</summary>
    public HashBuilder<T> AsSha256() { _algorithm = HashAlgorithmName.SHA256; _customHasher = null; return this; }
    /// <summary>Use SHA-1.</summary>
    public HashBuilder<T> AsSha1() { _algorithm = HashAlgorithmName.SHA1; _customHasher = null; return this; }
    /// <summary>Use SHA-512.</summary>
    public HashBuilder<T> AsSha512() { _algorithm = HashAlgorithmName.SHA512; _customHasher = null; return this; }
    /// <summary>Use MD5 (insecure — prefer for non-security identity only).</summary>
    public HashBuilder<T> AsMd5() { _algorithm = HashAlgorithmName.MD5; _customHasher = null; return this; }

    /// <summary>Plug in a custom hash function.</summary>
    public HashBuilder<T> Using(Func<string, string> hasher)
    {
        _customHasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        return this;
    }

    protected override void RegisterRule(AsyncPredicate<T> predicate)
    {
        var customHasher = _customHasher;
        var algorithm = _algorithm;

        Filter.AddRule(new PropertyMutationRule<T>(
            new[] { _accessor },
            predicate,
            valueProducer: (instance, accessor, _) =>
            {
                var current = accessor.GetValue(instance);
                if (current is not string s) return current;          // null or non-string → pass through
                if (string.IsNullOrEmpty(s)) return s;                  // empty stays empty
                return customHasher != null ? customHasher(s) : HashHex(s, algorithm);
            }));
    }

    internal static string HashHex(string input, HashAlgorithmName algorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = algorithm.Name switch
        {
            "SHA256" => SHA256.HashData(bytes),
            "SHA1" => SHA1.HashData(bytes),
            "SHA512" => SHA512.HashData(bytes),
            "MD5" => MD5.HashData(bytes),
            _ => throw new NotSupportedException($"Hash algorithm {algorithm.Name} is not supported."),
        };
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
