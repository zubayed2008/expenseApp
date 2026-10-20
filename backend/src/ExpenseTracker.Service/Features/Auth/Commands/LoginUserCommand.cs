using MediatR;

namespace ExpenseTracker.Service.Features.Auth.Commands;

/// <summary>Body of POST /api/auth/login (FR-201).</summary>
public sealed record LoginUserCommand(string Email, string Password)
    : IRequest<LoginResponse>;
