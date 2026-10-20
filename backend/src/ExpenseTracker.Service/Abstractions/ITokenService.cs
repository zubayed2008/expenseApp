using ExpenseTracker.Domain;

namespace ExpenseTracker.Service.Abstractions;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) IssueToken(User user);
}
