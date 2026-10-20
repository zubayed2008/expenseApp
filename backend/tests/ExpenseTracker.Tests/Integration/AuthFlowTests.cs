using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExpenseTracker.Domain;
using ExpenseTracker.Tests.Integration;

/// <summary>
/// End-to-end auth flow against real PostgreSQL (Phase 5 acceptance criteria).
/// Each test uses a unique email to avoid cross-test conflicts.
/// </summary>
public class AuthFlowTests(ExpenseTrackerApiFactory factory) : IClassFixture<ExpenseTrackerApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static async Task<string> RegisterAndLoginAsync(
        HttpClient client, string email, string password = "correct-horse-battery")
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            Currency = "USD",
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        return body.GetProperty("accessToken").GetString()!;
    }

    [SkippableFact]
    public async Task Register_creates_the_account_and_seeds_default_categories()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = "correct-horse-battery",
            Currency = "USD",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var userId = Guid.Parse(body.GetProperty("id").GetString()!);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("USD", body.GetProperty("currency").GetString());

        // FR-223: 12 expense + 8 income default categories seeded for this account only.
        await using var db = factory.CreateDbContext();
        var user = await db.Users.FindAsync(userId);
        var categories = db.Categories.Where(category => category.UserId == userId).ToList();
        Assert.Equal(20, categories.Count);
        Assert.Equal(12, categories.Count(category => category.Kind == TransactionKind.Expense));
        Assert.Equal(8, categories.Count(category => category.Kind == TransactionKind.Income));
        Assert.NotNull(user);
        Assert.NotEmpty(user.PasswordHash);
        Assert.DoesNotContain("correct", user.PasswordHash); // hashed, not plaintext
    }

    [SkippableFact]
    public async Task Register_rejects_a_duplicate_email_with_409()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();
        var email = UniqueEmail();

        var first = await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "correct-horse-battery" });
        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = "another-password-1" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [SkippableFact]
    public async Task Register_rejects_short_passwords_with_field_errors()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { Email = "bad", Password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var errors = body.GetProperty("errors");
        Assert.True(errors.GetProperty("email").GetArrayLength() > 0);
        Assert.True(errors.GetProperty("password").GetArrayLength() > 0);
    }
}

public class AuthFlowTestsPart2(ExpenseTrackerApiFactory factory) : IClassFixture<ExpenseTrackerApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static async Task<string> RegisterAndLoginAsync(
        HttpClient client, string email, string password = "correct-horse-battery")
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            Currency = "USD",
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(Json);
        return body.GetProperty("accessToken").GetString()!;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [SkippableFact]
    public async Task Login_returns_a_token_and_me_honors_it()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();
        var email = UniqueEmail();
        var token = await RegisterAndLoginAsync(client, email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var body = await me.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(email, body.GetProperty("email").GetString());
    }

    [SkippableFact]
    public async Task Login_with_a_wrong_password_is_unauthorized()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAndLoginAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Me_without_a_token_is_unauthorized()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [SkippableFact]
    public async Task Accounts_are_isolated_per_user()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();
        var emailA = UniqueEmail();
        var emailB = UniqueEmail();

        var tokenA = await RegisterAndLoginAsync(client, emailA);
        await RegisterAndLoginAsync(client, emailB);

        // FR-202: user A's token only ever resolves to user A.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var me = await client.GetAsync("/api/auth/me");
        var body = await me.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(emailA, body.GetProperty("email").GetString());

        // FR-202 at the data layer: each account's categories are its own.
        await using var db = factory.CreateDbContext();
        var userA = db.Users.Single(user => user.Email == emailA);
        var userB = db.Users.Single(user => user.Email == emailB);
        var categoriesA = db.Categories.Where(category => category.UserId == userA.Id).ToList();
        var categoriesB = db.Categories.Where(category => category.UserId == userB.Id).ToList();
        Assert.Equal(20, categoriesA.Count);
        Assert.Equal(20, categoriesB.Count);
        Assert.Empty(categoriesA.Select(category => category.Id).Intersect(categoriesB.Select(category => category.Id)));
    }

    [SkippableFact]
    public async Task Health_report_is_healthy()
    {
        Skip.IfNot(DockerEnvironment.IsAvailable, "Docker is not available in this environment.");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

