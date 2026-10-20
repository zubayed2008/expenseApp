using System.Security.Claims;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ExpenseTracker.Api.Endpoints;

public sealed record RegisterRequest(string Email, string Password, string? Currency);

public sealed record RegisterResponse(Guid Id, string Email, string Currency);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);

public sealed record MeResponse(Guid Id, string Email, string Currency, DateTime CreatedAt);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (!ValidationRules.IsValidEmail(request.Email))
            {
                errors["email"] = ["Provide a valid email address."];
            }

            if (!ValidationRules.IsValidPassword(request.Password))
            {
                errors["password"] =
                    [$"Password must be {ValidationRules.MinimumPasswordLength}-{ValidationRules.MaximumPasswordLength} characters."];
            }

            var currency = request.Currency?.Trim().ToUpperInvariant() ?? "USD";
            if (!ValidationRules.IsValidCurrencyCode(currency))
            {
                errors["currency"] = ["Currency must be a 3-letter code like USD or GBP."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var outcome = await accounts.RegisterAsync(request.Email, request.Password, currency, cancellationToken);
            if (outcome.EmailTaken)
            {
                return Results.Conflict(new { errors = new Dictionary<string, string[]>
                {
                    ["email"] = ["An account with this email already exists."],
                } });
            }

            return Results.Created($"/auth/me", new RegisterResponse(
                outcome.User!.Id,
                outcome.User.Email,
                outcome.User.Currency));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            AccountService accounts,
            JwtTokenService tokens,
            CancellationToken cancellationToken) =>
        {
            var user = await accounts.AuthenticateAsync(request.Email, request.Password, cancellationToken);
            if (user is null)
            {
                return Results.Problem(
                    title: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var (accessToken, expiresAtUtc) = tokens.IssueToken(user);
            return Results.Ok(new LoginResponse(accessToken, expiresAtUtc));
        });

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            var userId = ReadUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var user = await accounts.FindByIdAsync(userId.Value, cancellationToken);
            return user is null
                ? Results.Unauthorized()
                : Results.Ok(new MeResponse(user.Id, user.Email, user.Currency, user.CreatedAt));
        }).RequireAuthorization();
    }

    /// <summary>The "sub" claim holds the user id (inbound claim mapping is off).</summary>
    internal static Guid? ReadUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
