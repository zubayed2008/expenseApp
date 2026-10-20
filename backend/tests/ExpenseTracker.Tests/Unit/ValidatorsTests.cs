using ExpenseTracker.Service.Features.Auth.Commands;
using FluentValidation;
using FluentValidation.TestHelper;

namespace ExpenseTracker.Tests.Unit;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Accepts_a_valid_registration()
    {
        var result = _validator.TestValidate(new RegisterUserCommand(
            "user@example.com", "correct-horse-battery", "BDT"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Accepts_a_missing_currency_which_defaults_to_USD()
    {
        var result = _validator.TestValidate(new RegisterUserCommand(
            "user@example.com", "correct-horse-battery", null));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_bad_emails_passwords_and_currencies_with_field_errors()
    {
        var result = _validator.TestValidate(new RegisterUserCommand(
            "not-an-email", "short", "USDD"));

        result.ShouldHaveValidationErrorFor(command => command.Email);
        result.ShouldHaveValidationErrorFor(command => command.Password);
        result.ShouldHaveValidationErrorFor(command => command.Currency);
    }
}

public class LoginUserCommandValidatorTests
{
    private readonly LoginUserCommandValidator _validator = new();

    [Fact]
    public void Accepts_valid_credentials()
    {
        var result = _validator.TestValidate(new LoginUserCommand(
            "user@example.com", "correct-horse-battery"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_empty_fields()
    {
        var result = _validator.TestValidate(new LoginUserCommand("", ""));

        result.ShouldHaveValidationErrorFor(command => command.Email);
        result.ShouldHaveValidationErrorFor(command => command.Password);
    }
}
