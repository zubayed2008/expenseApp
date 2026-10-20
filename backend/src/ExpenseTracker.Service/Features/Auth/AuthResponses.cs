namespace ExpenseTracker.Service.Features.Auth;

public sealed record RegisterUserResponse(Guid Id, string Email, string Currency);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

public sealed record CurrentUserResponse(Guid Id, string Email, string Currency, DateTime CreatedAt);
