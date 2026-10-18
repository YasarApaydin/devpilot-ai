using Microsoft.Extensions.Configuration;

namespace WebChatAI.Application.Common;

public static class AppEnvironment
{
    public const string DefaultDevJwtSecret = "devpilot_super_secret_jwt_signing_key_2026_at_least_32_characters_long!";
    public const string DefaultDevEncryptionSecret = "devpilot_super_secret_api_key_vault_master_key_2026_at_least_32_chars!";

    private static readonly HashSet<string> ProductionLikeEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Production",
        "Prod",
        "Staging",
        "Stage",
        "Uat",
        "PreProd",
        "Release"
    };

    private static readonly HashSet<string> DevelopmentLikeEnvironments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Development",
        "Dev",
        "Local",
        "Test",
        "Testing"
    };

    private static readonly HashSet<string> KnownStaticJwtDefaults = new(StringComparer.Ordinal)
    {
        DefaultDevJwtSecret,
        "devpilot_local_jwt_secret_signing_key_32bytes_long_2026!",
        DefaultDevEncryptionSecret,
        "devpilot_local_aes256_master_encryption_secret_2026!",
        "YOUR_SUPER_SECRET_JWT_KEY_HERE_AT_LEAST_32_CHARS",
        "CHANGE_ME"
    };

    private static readonly HashSet<string> KnownStaticEncryptionDefaults = new(StringComparer.Ordinal)
    {
        DefaultDevEncryptionSecret,
        "devpilot_local_aes256_master_encryption_secret_2026!",
        "devpilot_api_key_vault_master_secret_2026_aes256_key!",
        DefaultDevJwtSecret,
        "devpilot_local_jwt_secret_signing_key_32bytes_long_2026!",
        "DevPilotDefaultApiKeyEncryptionKey32CharsLong!",
        "DevPilotAI_SuperSecret_AES256_MasterKey_2026!",
        "CHANGE_ME"
    };

    public static string ResolveCanonicalEnvironmentName(IConfiguration? configuration, string? hostEnvironmentName = null)
    {
        // Single authoritative source of truth resolution: the first non-empty source dictates the environment
        string? canonicalEnv = null;

        if (!string.IsNullOrWhiteSpace(hostEnvironmentName))
        {
            canonicalEnv = hostEnvironmentName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(configuration?["ASPNETCORE_ENVIRONMENT"]))
        {
            canonicalEnv = configuration["ASPNETCORE_ENVIRONMENT"]!.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
        {
            canonicalEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")!.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(configuration?["DOTNET_ENVIRONMENT"]))
        {
            canonicalEnv = configuration["DOTNET_ENVIRONMENT"]!.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")))
        {
            canonicalEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")!.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(configuration?["DEPLOYMENT_STAGE"]))
        {
            canonicalEnv = configuration["DEPLOYMENT_STAGE"]!.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEPLOYMENT_STAGE")))
        {
            canonicalEnv = Environment.GetEnvironmentVariable("DEPLOYMENT_STAGE")!.Trim();
        }

        // Fail-closed default: if no source defines an environment, it is strictly Production
        return !string.IsNullOrWhiteSpace(canonicalEnv) ? canonicalEnv : "Production";
    }

    public static string GetEnvironmentName(IConfiguration? configuration, string? hostEnvironmentName = null)
    {
        return ResolveCanonicalEnvironmentName(configuration, hostEnvironmentName);
    }

    public static bool IsProductionLike(IConfiguration? configuration, string? hostEnvironmentName = null)
    {
        var canonical = ResolveCanonicalEnvironmentName(configuration, hostEnvironmentName);

        // Fail-closed: only explicitly recognized Development environments are treated as development.
        // Everything else (Production, Staging, UAT, Unknown, Empty) is treated as production-like.
        return !DevelopmentLikeEnvironments.Contains(canonical);
    }

    public static bool IsDevelopment(IConfiguration? configuration, string? hostEnvironmentName = null)
    {
        return !IsProductionLike(configuration, hostEnvironmentName);
    }

    public static string ValidateAndResolveJwtSecret(IConfiguration configuration, string? hostEnvironmentName = null)
    {
        var isProdLike = IsProductionLike(configuration, hostEnvironmentName);
        var secret = configuration["Jwt:SecretKey"] ?? configuration["JWT_SECRET_KEY"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Trim().Length < 32)
        {
            if (isProdLike)
            {
                throw new InvalidOperationException(
                    "Fatal Security Error: Configuration key 'Jwt:SecretKey' (or 'JWT_SECRET_KEY') is missing or shorter than 32 characters in a production-like environment.");
            }
            return DefaultDevJwtSecret;
        }

        var trimmed = secret.Trim();
        if (isProdLike && KnownStaticJwtDefaults.Contains(trimmed))
        {
            throw new InvalidOperationException(
                "Fatal Security Error: Configuration key 'Jwt:SecretKey' cannot use a known static default placeholder in a production-like environment.");
        }

        return trimmed;
    }

    public static string ValidateAndResolveEncryptionSecret(IConfiguration configuration, string? hostEnvironmentName = null)
    {
        var isProdLike = IsProductionLike(configuration, hostEnvironmentName);
        var secret = configuration["Encryption:ApiKeySecret"] ?? configuration["ENCRYPTION_API_KEY_SECRET"];

        if (string.IsNullOrWhiteSpace(secret) || secret.Trim().Length < 32)
        {
            if (isProdLike)
            {
                throw new InvalidOperationException(
                    "Fatal Security Error: Configuration key 'Encryption:ApiKeySecret' (or 'ENCRYPTION_API_KEY_SECRET') is missing or shorter than 32 characters in a production-like environment.");
            }
            return DefaultDevEncryptionSecret;
        }

        var trimmed = secret.Trim();
        if (isProdLike && KnownStaticEncryptionDefaults.Contains(trimmed))
        {
            throw new InvalidOperationException(
                "Fatal Security Error: Configuration key 'Encryption:ApiKeySecret' cannot use a known static default placeholder in a production-like environment.");
        }

        return trimmed;
    }
}

