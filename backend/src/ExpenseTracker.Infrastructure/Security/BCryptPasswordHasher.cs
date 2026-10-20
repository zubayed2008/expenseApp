using ExpenseTracker.Service.Abstractions;

namespace ExpenseTracker.Infrastructure.Security;

/// <summary>
/// bcrypt (work factor 11). bcrypt embeds a per-hash salt, so hashing the
/// same password twice yields different hashes (NFR-010).
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
