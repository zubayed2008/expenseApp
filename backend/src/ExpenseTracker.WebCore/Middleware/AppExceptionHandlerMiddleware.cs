using System.Text.Json;
using ExpenseTracker.Service.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.WebCore.Middleware;

/// <summary>
/// Maps application exceptions to ProblemDetails responses:
/// ValidationException -> 400 with field errors,
/// AppConflictException -> 409,
/// AppUnauthorizedException -> 401.
/// </summary>
public sealed class AppExceptionHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (!TryMap(exception, context, out var statusCode, out var payload))
            {
                throw;
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(payload);
        }
    }

    private static bool TryMap(
        Exception exception,
        HttpContext context,
        out int statusCode,
        out object payload)
    {
        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                var errors = validationException.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => JsonNamingPolicy.CamelCase.ConvertName(group.Key),
                        group => group.Select(failure => failure.ErrorMessage).ToArray());
                payload = new
                {
                    title = "One or more validation errors occurred.",
                    status = statusCode,
                    errors,
                };
                return true;

            case AppConflictException conflictException:
                statusCode = StatusCodes.Status409Conflict;
                payload = new ProblemDetails
                {
                    Title = conflictException.Title,
                    Status = statusCode,
                    Instance = context.Request.Path,
                };
                return true;

            case AppUnauthorizedException unauthorizedException:
                statusCode = StatusCodes.Status401Unauthorized;
                payload = new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = unauthorizedException.Reason,
                    Status = statusCode,
                    Instance = context.Request.Path,
                };
                return true;

            default:
                statusCode = 0;
                payload = null!;
                return false;
        }
    }
}
