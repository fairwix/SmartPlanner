using System.Text;
using Microsoft.Extensions.Configuration;

namespace SmartPlanner.Application.Common.Security;

public static class JwtSecretValidator
{
    private const int MinimumSecretLengthInBytes = 32;

    private static readonly HashSet<string> KnownInsecureSecrets = new(StringComparer.Ordinal)
    {
        "your-32-character-secret-key-change-in-production",
        "dev-secret-key-123456789012345678901234567890"
    };

    public static string GetRequiredSecret(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var secret = configuration["JwtSettings:Secret"];
        if (string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetByteCount(secret) < MinimumSecretLengthInBytes ||
            KnownInsecureSecrets.Contains(secret))
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret must be configured with a private, randomly generated key of at least 32 bytes. " +
                "Set it through a secret store or the JwtSettings__Secret environment variable.");
        }

        return secret;
    }
}
