# Encryption and Security Reference

This page documents the encryption and security utilities available in Nextended.Core.

## Overview

Nextended provides built-in encryption classes for securing sensitive string data using industry-standard algorithms:
- **AesEncryption** - AES encryption with PBKDF2 key derivation
- **RijndaelEncryption** - Rijndael (AES) encryption with CBC mode

Both classes implement the `IStringEncryption` interface for consistent usage patterns.

---

## AesEncryption

**Namespace**: `Nextended.Core.Encryption`

Provides string encryption and decryption using the AES (Advanced Encryption Standard) algorithm with PBKDF2 (Password-Based Key Derivation Function 2) for secure key generation.

### Features

- **256-bit key size** for strong encryption
- **PBKDF2 key derivation** with configurable iterations
- **Automatic IV generation** for each encryption operation
- **Configurable salt** for key derivation
- **Base64 encoding** of encrypted output

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Salt` | `byte[]` | Default salt | Salt used for PBKDF2 key derivation |
| `Iterations` | `int` | 1223 | Number of PBKDF2 iterations |

### Methods

| Method | Description |
|--------|-------------|
| `Encrypt(string clearText, string key)` | Encrypts text and returns Base64 string |
| `Decrypt(string cipherText, string key)` | Decrypts Base64 string back to text |

### Example Usage

```csharp
using Nextended.Core.Encryption;

var aes = new AesEncryption();

// Encrypt sensitive data
string password = "MySecretPassword";
string key = "MyEncryptionKey123";
string encrypted = aes.Encrypt(password, key);
Console.WriteLine($"Encrypted: {encrypted}");

// Decrypt data
string decrypted = aes.Decrypt(encrypted, key);
Console.WriteLine($"Decrypted: {decrypted}"); // "MySecretPassword"

// Custom configuration
var customAes = new AesEncryption 
{ 
    Iterations = 10000  // Higher iterations = more secure but slower
};
string secureEncrypted = customAes.Encrypt("SensitiveData", key);
```

### Security Considerations

1. **Key Storage**: Never store encryption keys in source code. Use secure key management systems.
2. **Iterations**: Higher iteration counts increase security but reduce performance. Balance based on your needs.
3. **Salt**: While a default salt is provided, consider using unique salts per user/application for enhanced security.

---

## RijndaelEncryption

**Namespace**: `Nextended.Core.Encryption`

Provides string encryption and decryption using the Rijndael algorithm (the basis for AES) with CBC (Cipher Block Chaining) mode and PKCS7 padding.

### Features

- **128-bit block size** and **128-bit key size**
- **Random salt and IV** generation for each encryption
- **CBC mode** with PKCS7 padding
- **RFC2898 key derivation** with configurable iterations
- **Base64 encoding** of encrypted output

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Iterations` | `int` | 1347 | Number of iterations for key derivation |

### Methods

| Method | Description |
|--------|-------------|
| `Encrypt(string str, string key)` | Encrypts string and returns Base64 string |
| `Decrypt(string str, string key)` | Decrypts Base64 string back to original text |

### Example Usage

```csharp
using Nextended.Core.Encryption;

var rijndael = new RijndaelEncryption();

// Encrypt credit card number
string cardNumber = "4111-1111-1111-1111";
string encryptionKey = "StrongKey!123";
string encrypted = rijndael.Encrypt(cardNumber, encryptionKey);
Console.WriteLine($"Encrypted card: {encrypted}");

// Decrypt when needed
string decrypted = rijndael.Decrypt(encrypted, encryptionKey);
Console.WriteLine($"Decrypted card: {decrypted}");

// Adjust security level
var secureRijndael = new RijndaelEncryption 
{ 
    Iterations = 5000 
};
string moreSecure = secureRijndael.Encrypt("TopSecret", encryptionKey);
```

### Technical Details

- Each encryption generates a **random salt** and **random IV** (Initialization Vector)
- The salt and IV are **prepended** to the ciphertext, allowing for decryption without storing them separately
- Key is derived using **Rfc2898DeriveBytes** (PBKDF2) with the provided password and random salt

---

## IStringEncryption Interface

**Namespace**: `Nextended.Core.Contracts`

Common interface implemented by all encryption classes, enabling polymorphic usage.

```csharp
public interface IStringEncryption
{
    string Encrypt(string clearText, string key);
    string Decrypt(string cipherText, string key);
    int Iterations { get; set; }
}
```

### Example Usage

```csharp
using Nextended.Core.Contracts;
using Nextended.Core.Encryption;

// Use interface for flexibility
IStringEncryption encryption;

if (useAes)
    encryption = new AesEncryption();
else
    encryption = new RijndaelEncryption();

// Common usage pattern
string encrypted = encryption.Encrypt("MyData", "MyKey");
string decrypted = encryption.Decrypt(encrypted, "MyKey");
```

---

## Best Practices

### Key Management

1. **Never hardcode keys** in source code
2. Use **environment variables** or **secure key vaults** (Azure Key Vault, AWS KMS, etc.)
3. Implement **key rotation** policies for long-lived applications
4. Use **different keys** for different security domains

```csharp
// Good: Key from secure source
string key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
if (string.IsNullOrEmpty(key))
    throw new InvalidOperationException("Encryption key not configured");

var aes = new AesEncryption();
string encrypted = aes.Encrypt(sensitiveData, key);
```

### Data Storage

1. **Always encrypt** sensitive data before storage
2. Consider **field-level encryption** for databases
3. Use **encryption at rest** for file storage
4. Implement **secure deletion** for decrypted data in memory

```csharp
using Nextended.Core.Encryption;

public class UserRepository
{
    private readonly IStringEncryption _encryption;
    private readonly string _key;

    public UserRepository(IStringEncryption encryption, string key)
    {
        _encryption = encryption;
        _key = key;
    }

    public void SaveUser(User user)
    {
        // Encrypt sensitive fields before saving
        var encryptedUser = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = _encryption.Encrypt(user.Email, _key),
            SSN = _encryption.Encrypt(user.SSN, _key)
        };
        
        // Save to database
        SaveToDatabase(encryptedUser);
    }

    public User GetUser(int id)
    {
        var dto = LoadFromDatabase(id);
        
        // Decrypt sensitive fields
        return new User
        {
            Id = dto.Id,
            Username = dto.Username,
            Email = _encryption.Decrypt(dto.Email, _key),
            SSN = _encryption.Decrypt(dto.SSN, _key)
        };
    }
}
```

### Configuration

1. **Increase iterations** for better security (at the cost of performance)
2. **Test performance** impact before deploying to production
3. Use **consistent settings** across encryption and decryption
4. **Document** your security configuration decisions

```csharp
// Development: Lower iterations for faster testing
#if DEBUG
var encryption = new AesEncryption { Iterations = 1000 };
#else
// Production: Higher iterations for security
var encryption = new AesEncryption { Iterations = 10000 };
#endif
```

### Error Handling

1. **Never expose** encryption details in error messages
2. **Log security events** (failed decryption attempts, etc.)
3. Implement **rate limiting** to prevent brute force attacks
4. Use **generic error messages** to avoid information leakage

```csharp
try
{
    string decrypted = encryption.Decrypt(cipherText, key);
    return decrypted;
}
catch (CryptographicException)
{
    // Log for security monitoring
    _logger.LogWarning("Failed decryption attempt");
    
    // Generic error to user
    throw new InvalidOperationException("Unable to decrypt data");
}
```

---

## Choosing Between AES and Rijndael

Both implementations are secure, but have subtle differences:

### Use AesEncryption when:
- You need **modern .NET APIs** (available in newer frameworks)
- You want **SHA-512** key derivation (non-.NET Standard 2.0)
- You need **maximum compatibility** with other systems

### Use RijndaelEncryption when:
- You need **explicit control** over block size
- You're working with **legacy systems** that require Rijndael
- You need **CBC mode** with specific configurations

### For Most Applications:
**AesEncryption** is recommended as it uses modern cryptographic best practices and will receive ongoing security updates.

---

## Performance Considerations

### Iteration Count Impact

Higher iterations increase security but reduce performance:

| Iterations | Relative Speed | Security Level |
|------------|----------------|----------------|
| 1,000 | Fastest | Basic |
| 10,000 | Medium | Recommended |
| 100,000 | Slower | High Security |
| 1,000,000 | Slowest | Maximum |

### Benchmarks (Approximate)

On typical hardware:
- **1,000 iterations**: ~1-2ms per encryption
- **10,000 iterations**: ~10-20ms per encryption  
- **100,000 iterations**: ~100-200ms per encryption

### Optimization Tips

1. **Cache encryption instances** - creating new instances is expensive
2. **Batch encrypt** when possible
3. **Consider async operations** for UI applications
4. **Profile** your specific use case before optimizing

```csharp
// Good: Reuse instance
private static readonly AesEncryption _encryption = new AesEncryption();

public string EncryptData(string data, string key)
{
    return _encryption.Encrypt(data, key);
}

// Async for UI responsiveness
public async Task<string> EncryptDataAsync(string data, string key)
{
    return await Task.Run(() => _encryption.Encrypt(data, key));
}
```

---

## See Also

- [Helper Utilities Reference](helpers.md)
- [Extension Methods Reference](extensions.md)
- [Nextended.Core Documentation](../projects/core.md)
