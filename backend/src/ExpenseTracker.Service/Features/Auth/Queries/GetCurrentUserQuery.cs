using ExpenseTracker.Service.Abstractions;
using ExpenseTracker.Service.Exceptions;
using MediatR;

namespace ExpenseTracker.Service.Features.Auth.Queries;

/// <summary>GET /api/auth/me — resolves the current user from the "sub" claim.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserResponse>;

public sealed class GetCurrentUserQueryHandler(IUserRepository users)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<CurrentUserResponse> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new AppUnauthorizedException("Account not found.");
        }

        return new CurrentUserResponse(user.Id, user.Email, user.Currency, user.CreatedAt);
    }
}
