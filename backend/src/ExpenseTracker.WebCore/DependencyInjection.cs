using System.Text;
using ExpenseTracker.Service.Options;
using ExpenseTracker.WebCore.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.WebCore;

public static class DependencyInjection
{
    /// <summary>
    /// Shared web plumbing: JWT bearer authentication wired to JwtOptions,
    /// and the application exception-to-ProblemDetails middleware.
    /// </summary>
    public static IServiceCollection AddWebCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep raw claim names ("sub")
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                };
            });
        services.AddAuthorization();

        return services;
    }

    /// <summary>Registers the exception-to-ProblemDetails middleware.</summary>
    public static IApplicationBuilder UseWebCore(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AppExceptionHandlerMiddleware>();
    }
}
