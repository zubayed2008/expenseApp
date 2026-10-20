using ExpenseTracker.Domain;

namespace ExpenseTracker.Tests.Unit;

public class DefaultCategoriesTests
{
    [Fact]
    public void Category_names_are_unique_per_kind()
    {
        var duplicates = DefaultCategories.All
            .GroupBy(category => (category.Name, category.Kind))
            .Where(group => group.Count() > 1)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Seeding_matches_the_legacy_CLI_lists()
    {
        var expense = DefaultCategories.All.Where(c => c.Kind == TransactionKind.Expense).Select(c => c.Name).ToList();
        var income = DefaultCategories.All.Where(c => c.Kind == TransactionKind.Income).Select(c => c.Name).ToList();

        // 12 expense + 8 income categories, mirroring the v1 CLI.
        Assert.Equal(12, expense.Count);
        Assert.Equal(8, income.Count);
        Assert.Contains("Food", expense);
        Assert.Contains("Salary", income);
        Assert.Contains("Freelance", income);
    }
}
