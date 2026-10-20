namespace ExpenseTracker.Service.Exceptions;

/// <summary>Mapped to HTTP 409 Conflict by the WebCore exception middleware.</summary>
public sealed class AppConflictException(string title) : Exception(title)
{
    public string Title => title;
}

/// <summary>Mapped to HTTP 401 Unauthorized by the WebCore exception middleware.</summary>
public sealed class AppUnauthorizedException(string message) : Exception(message)
{
    public string Reason => message;
}
