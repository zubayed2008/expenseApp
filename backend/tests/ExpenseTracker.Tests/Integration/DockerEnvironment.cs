namespace ExpenseTracker.Tests.Integration;

/// <summary>
/// Detects whether integration tests can run: either a Docker daemon (for
/// Testcontainers) or an existing PostgreSQL provided via the
/// EXPENSE_TRACKER_TEST_CONNECTION environment variable. Tests skip
/// gracefully otherwise; CI runners have Docker, so they always execute.
/// </summary>
public static class DockerEnvironment
{
    public static bool IsAvailable =>
        File.Exists("/var/run/docker.sock")
        || Environment.GetEnvironmentVariable("DOCKER_HOST") is not null
        || Environment.GetEnvironmentVariable("EXPENSE_TRACKER_TEST_CONNECTION") is not null;
}
