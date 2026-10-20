using System.Text.RegularExpressions;

namespace ExpenseTracker.Domain;

/// <summary>
/// Domain validation rules ported from the legacy CLI (FR-320/321) so the
/// server enforces exactly what v1 enforced.
/// </summary>
public static partial class ValidationRules
{
    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")]
    private static partial Regex EmailRegex();

    public const int MinimumPasswordLength = 8;

    public const int MaximumPasswordLength = 128;

    public const int DescriptionMaxLength = 500;

    public const int PaymentMethodMaxLength = 100;

    public const int CategoryNameMaxLength = 100;

    /// <summary>Amount must be positive with at most two decimals (FR-221).</summary>
    public static bool IsValidAmount(decimal amount) =>
        amount > 0 && decimal.Round(amount, 2) == amount;

    /// <summary>Dates must be provided; impossible calendar dates are rejected earlier by JSON parsing.</summary>
    public static bool IsValidDate(DateOnly date) => date != default;

    /// <summary>Email policy: single @, non-empty local part and domain (FR-200).</summary>
    public static bool IsValidEmail(string email) =>
        email.Length <= 256 && EmailRegex().IsMatch(email);

    /// <summary>Password policy (FR-200).</summary>
    public static bool IsValidPassword(string password) =>
        password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength;

    /// <summary>ISO-4217-style three-letter code (FR-203).</summary>
    public static bool IsValidCurrencyCode(string code) =>
        code.Length == 3 && code.All(char.IsLetter) && code == code.ToUpperInvariant();

    public static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public static string NormalizeCurrency(string code) =>
        code.Trim().ToUpperInvariant();
}
