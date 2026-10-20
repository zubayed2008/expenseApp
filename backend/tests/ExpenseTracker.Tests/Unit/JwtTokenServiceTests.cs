using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.Api.Models;
using ExpenseTracker.Api.Options;
using ExpenseTracker.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Tests.Unit;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryMinutes = 60) =>
        new(Options.Create(new JwtOptions
        {
            SigningKey = "test-signing-key-with-32-characters!!",
            Issuer = "expense-tracker",
            Audience = "expense-tracker-clients",
            ExpiryMinutes = expiryMinutes,
        }));

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
    };

    [Fact]
    public void IssueToken_embeds_sub_and_email_claims()
    {
        var user = CreateUser();
        var (token, expiresAt) = CreateService().IssueToken(user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sub = jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value;
        var email = jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value;

        Assert.Equal(user.Id.ToString(), sub);
        Assert.Equal(user.Email, email);
        Assert.Equal("expense-tracker", jwt.Issuer);
        Assert.Equal("expense-tracker-clients", jwt.Audiences.Single());
        // JWT expiry ("exp") is expressed in whole Unix seconds.
        Assert.True((jwt.ValidTo - expiresAt).Duration() <= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IssueToken_expires_after_the_configured_minutes()
    {
        var (token, expiresAt) = CreateService(expiryMinutes: 30).IssueToken(CreateUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(DateTime.UtcNow.AddMinutes(30), expiresAt, precision: TimeSpan.FromSeconds(5));
        Assert.True((jwt.ValidTo - expiresAt).Duration() <= TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IssueToken_rejects_keys_shorter_than_32_characters()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            SigningKey = "too-short",
        }));

        Assert.Throws<InvalidOperationException>(() => service.IssueToken(CreateUser()));
    }

    [Fact]
    public void IssueToken_tokens_validate_against_the_same_key()
    {
        var user = CreateUser();
        var (token, _) = CreateService().IssueToken(user);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "expense-tracker",
            ValidateAudience = true,
            ValidAudience = "expense-tracker-clients",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-signing-key-with-32-characters!!")),
        };

        // JwtSecurityTokenHandler maps "sub" to ClaimTypes.NameIdentifier by default.
        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        Assert.Equal(user.Id.ToString(), sub);
    }
}
