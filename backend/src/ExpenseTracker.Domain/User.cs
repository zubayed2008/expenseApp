namespace ExpenseTracker.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized (trimmed, lower-cased) email; unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Salted bcrypt hash. Plaintext passwords are never stored (NFR-010).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>ISO-4217 code, e.g. USD. One currency per account (C-3).</summary>
    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<Category> Categories { get; set; } = [];

    public List<Transaction> Transactions { get; set; } = [];
}
