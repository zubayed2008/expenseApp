using ExpenseTracker.Domain;
using FluentValidation;

namespace ExpenseTracker.Service.Features.Auth.Commands;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Provide a valid email address.")
            .Must(ValidationRules.IsValidEmail).WithMessage("Provide a valid email address.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Provide a password.");
    }
}
