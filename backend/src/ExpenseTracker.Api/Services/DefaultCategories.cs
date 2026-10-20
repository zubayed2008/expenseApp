using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Services;

/// <summary>
/// Default categories seeded per account at registration (FR-223) —
/// ported 1:1 from the legacy CLI's category lists.
/// </summary>
public static class DefaultCategories
{
    public static readonly (string Name, TransactionKind Kind)[] All =
    [
        ("Food", TransactionKind.Expense),
        ("Groceries", TransactionKind.Expense),
        ("Rent", TransactionKind.Expense),
        ("Utilities", TransactionKind.Expense),
        ("Transport", TransactionKind.Expense),
        ("Health", TransactionKind.Expense),
        ("Shopping", TransactionKind.Expense),
        ("Entertainment", TransactionKind.Expense),
        ("Travel", TransactionKind.Expense),
        ("Education", TransactionKind.Expense),
        ("Subscriptions", TransactionKind.Expense),
        ("Other", TransactionKind.Expense),
        ("Salary", TransactionKind.Income),
        ("Bonus", TransactionKind.Income),
        ("Freelance", TransactionKind.Income),
        ("Business", TransactionKind.Income),
        ("Investments", TransactionKind.Income),
        ("Gifts", TransactionKind.Income),
        ("Refunds", TransactionKind.Income),
        ("Other", TransactionKind.Income),
    ];
}
