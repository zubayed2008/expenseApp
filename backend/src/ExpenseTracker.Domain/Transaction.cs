namespace ExpenseTracker.Domain;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>Expense or income (FR-220). Amount is positive; sign derives from Type.</summary>
    public TransactionKind Type { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Always positive, at most two decimals; numeric(18,2) (C-2, FR-221).</summary>
    public decimal Amount { get; set; }

    public Guid CategoryId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
