using MediatR;

namespace ExpenseTracker.Service.Features.Auth.Commands;

/// <summary>Body of POST /api/auth/register (FR-200/203).</summary>
public sealed record RegisterUserCommand(string Email, string Password, string? Currency)
    : IRequest<RegisterUserResponse>;
