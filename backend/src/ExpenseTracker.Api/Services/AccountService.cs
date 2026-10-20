using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Services;

public sealed class AccountService(AppDbContext db, IPasswordHasher passwordHasher)
{
    public sealed record RegisterOutcome(User? User, bool EmailTaken);

    /// <summary>Creates the account and seeds default categories (FR-223).</summary>
    public async Task<RegisterOutcome> RegisterAsync(
        string email,
        string password,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var normalized = ValidationRules.NormalizeEmail(email);
        if (await db.Users.AnyAsync(user => user.Email == normalized, cancellationToken))
        {
            return new RegisterOutcome(User: null, EmailTaken: true);
        }

        var user = new User
        {
            Email = normalized,
            PasswordHash = passwordHasher.Hash(password),
            Currency = currency,
        };

        user.Categories.AddRange(DefaultCategories.All
            .Select(category => new Category
            {
                UserId = user.Id,
                Name = category.Name,
                Kind = category.Kind,
            }));

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new RegisterOutcome(User: user, EmailTaken: false);
    }

    /// <summary>Returns the user when email+password match; otherwise null.</summary>
    public async Task<User?> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalized = ValidationRules.NormalizeEmail(email);
        var user = await db.Users.SingleOrDefaultAsync(
            user => user.Email == normalized,
            cancellationToken);
        if (user is null || !passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
}
