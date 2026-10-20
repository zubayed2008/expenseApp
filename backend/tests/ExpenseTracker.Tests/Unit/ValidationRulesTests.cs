using ExpenseTracker.Api.Services;

namespace ExpenseTracker.Tests.Unit;

public class ValidationRulesTests
{
    [Theory]
    [InlineData("12.50", true)]
    [InlineData("0.01", true)]
    [InlineData("0", false)]
    [InlineData("-5", false)]
    [InlineData("12.345", false)] // more than two decimals
    public void IsValidAmount_matches_the_legacy_CLI_rules(string text, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidAmount(decimal.Parse(text)));
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("first.last+tag@domain.co", true)]
    [InlineData("not-an-email", false)]
    [InlineData("@missing-local.com", false)]
    [InlineData("missing-at-sign.com", false)]
    [InlineData("user@", false)]
    public void IsValidEmail_checks_the_shape(string email, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("short", false)]
    [InlineData("12345678", true)]
    [InlineData("a-very-long-password-that-is-fine-for-bcrypt", true)]
    public void IsValidPassword_enforces_the_minimum_length(string password, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidPassword(password));
    }

    [Theory]
    [InlineData("USD", true)]
    [InlineData("usd", false)] // must be upper-case after caller normalization
    [InlineData("US", false)]
    [InlineData("USDD", false)]
    [InlineData("EU1", false)]
    public void IsValidCurrencyCode_requires_three_uppercase_letters(string code, bool expected)
    {
        Assert.Equal(expected, ValidationRules.IsValidCurrencyCode(code));
    }

    [Fact]
    public void NormalizeEmail_trims_and_lowercases()
    {
        Assert.Equal("user@example.com", ValidationRules.NormalizeEmail("  User@Example.COM "));
    }

    [Fact]
    public void IsValidDate_rejects_the_default_date()
    {
        Assert.True(ValidationRules.IsValidDate(new DateOnly(2026, 10, 20)));
        Assert.False(ValidationRules.IsValidDate(default));
    }
}
