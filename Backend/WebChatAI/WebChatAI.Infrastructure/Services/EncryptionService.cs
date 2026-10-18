using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    // Recommended cryptographic parameters for Argon2id:
    // Memory size: 19456 KB (~19 MB), Iterations (time): 2, Degree of Parallelism: 1
    // Salt: 16 bytes cryptographically secure random bytes
    // Hash length: 32 bytes (256 bits)
    private const int MemorySizeKb = 19456;
    private const int Iterations = 2;
    private const int DegreeOfParallelism = 1;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySizeKb,
            Iterations = Iterations
        };

        var hash = argon2.GetBytes(HashSize);

        // Standard modular crypt format: $argon2id$v=19$m=19456,t=2,p=1$<salt-b64>$<hash-b64>
        return $"$argon2id$v=19$m={MemorySizeKb},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        try
        {
            // 1. Argon2id verification
            if (passwordHash.StartsWith("$argon2id$", StringComparison.OrdinalIgnoreCase))
            {
                var parts = passwordHash.Split('$');
                if (parts.Length < 6)
                    return false;

                var paramParts = parts[3].Split(',');
                int memory = MemorySizeKb;
                int iterations = Iterations;
                int parallelism = DegreeOfParallelism;

                foreach (var param in paramParts)
                {
                    var kv = param.Split('=');
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "m" && int.TryParse(kv[1], out var m)) memory = m;
                        else if (kv[0] == "t" && int.TryParse(kv[1], out var t)) iterations = t;
                        else if (kv[0] == "p" && int.TryParse(kv[1], out var p)) parallelism = p;
                    }
                }

                var salt = Convert.FromBase64String(parts[4]);
                var expectedHash = Convert.FromBase64String(parts[5]);

                using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
                {
                    Salt = salt,
                    DegreeOfParallelism = parallelism,
                    MemorySize = memory,
                    Iterations = iterations
                };

                var computedHash = argon2.GetBytes(expectedHash.Length);
                return CryptographicOperations.FixedTimeEquals(expectedHash, computedHash);
            }

            // 2. Backward compatibility with existing BCrypt password hashes ($2a$, $2b$, $2y$, etc.)
            if (passwordHash.StartsWith("$2", StringComparison.OrdinalIgnoreCase))
            {
                return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
