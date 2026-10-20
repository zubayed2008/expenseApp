namespace ExpenseTracker.Tests.Integration;

/// <summary>
/// Detects whether a Docker daemon is available for Testcontainers.
/// Integration tests skip gracefully when it is not (e.g. restricted
/// sandboxes); CI runners have Docker, so they always execute.
/// </summary>
public static class DockerEnvironment
{
    public static bool IsAvailable =>
        File.Exists("/var/run/docker.sock")
        || Environment.GetEnvironmentVariable("DOCKER_HOST") is not null;
}
