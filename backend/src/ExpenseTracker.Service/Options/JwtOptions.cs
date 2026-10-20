namespace ExpenseTracker.Service.Options;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key, minimum 32 characters (NFR-013: set via environment in production).</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "expense-tracker";

    public string Audience { get; set; } = "expense-tracker-clients";

    public int ExpiryMinutes { get; set; } = 60;
}
