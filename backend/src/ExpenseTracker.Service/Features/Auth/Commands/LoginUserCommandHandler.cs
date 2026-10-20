using ExpenseTracker.Domain;
using ExpenseTracker.Service.Abstractions;
using ExpenseTracker.Service.Exceptions;
using MediatR;

namespace ExpenseTracker.Service.Features.Auth.Commands;

public sealed class LoginUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
    : IRequestHandler<LoginUserCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = ValidationRules.NormalizeEmail(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppUnauthorizedException("Invalid email or password.");
        }

        var (accessToken, expiresAtUtc) = tokenService.IssueToken(user);
        return new LoginResponse(accessToken, expiresAtUtc);
    }
}
