using ExpenseTracker.Domain;
using FluentValidation;

namespace ExpenseTracker.Service.Features.Auth.Commands;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Provide a valid email address.")
            .Must(ValidationRules.IsValidEmail).WithMessage("Provide a valid email address.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .Must(ValidationRules.IsValidPassword)
            .WithMessage($"Password must be {ValidationRules.MinimumPasswordLength}-{ValidationRules.MaximumPasswordLength} characters.");

        RuleFor(command => command.Currency)
            .Must(code => string.IsNullOrWhiteSpace(code) || ValidationRules.IsValidCurrencyCode(ValidationRules.NormalizeCurrency(code!)))
            .WithMessage("Currency must be a 3-letter code like USD or GBP.");
    }
}
