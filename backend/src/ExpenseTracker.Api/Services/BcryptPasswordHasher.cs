namespace ExpenseTracker.Api.Services;

/// <summary>
/// bcrypt (work factor 11). bcrypt embeds a per-hash salt, so hashing the
/// same password twice yields different hashes (NFR-010).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
