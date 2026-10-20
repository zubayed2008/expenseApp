using ExpenseTracker.Domain;
using ExpenseTracker.Service.Abstractions;
using ExpenseTracker.Service.Exceptions;
using MediatR;

namespace ExpenseTracker.Service.Features.Auth.Commands;

public sealed class RegisterUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = ValidationRules.NormalizeEmail(request.Email);
        if (await users.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new AppConflictException("An account with this email already exists.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "USD"
            : ValidationRules.NormalizeCurrency(request.Currency);

        var user = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Currency = currency,
        };

        // FR-223: seed the default category catalog for this account.
        user.Categories.AddRange(DefaultCategories.All.Select(category => new Category
        {
            UserId = user.Id,
            Name = category.Name,
            Kind = category.Kind,
        }));

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(user.Id, user.Email, user.Currency);
    }
}
