using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.Domain;
using ExpenseTracker.Service.Abstractions;
using ExpenseTracker.Service.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private const int MinimumKeyLength = 32;

    public (string AccessToken, DateTime ExpiresAtUtc) IssueToken(User user)
    {
        var settings = options.Value;
        if (settings.SigningKey.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumKeyLength} characters.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
