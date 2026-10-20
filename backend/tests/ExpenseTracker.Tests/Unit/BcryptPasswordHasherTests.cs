namespace ExpenseTracker.Tests.Unit;

public class BcryptPasswordHasherTests
{
    private readonly ExpenseTracker.Infrastructure.Security.BCryptPasswordHasher _hasher = new();

    [Theory]
    [InlineData("correct horse battery")]
    [InlineData("Tr0ub4dor&3")]
    public void Hash_produces_a_verifiable_hash(string password)
    {
        var hash = _hasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.StartsWith("$2", hash); // bcrypt format, embeds a salt
        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Hash_is_salted_so_identical_passwords_hash_differently()
    {
        var first = _hasher.Hash("hunter2hunter2");
        var second = _hasher.Hash("hunter2hunter2");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("hunter2hunter2", first));
        Assert.True(_hasher.Verify("hunter2hunter2", second));
    }

    [Fact]
    public void Verify_rejects_wrong_passwords()
    {
        var hash = _hasher.Hash("right-password-1");

        Assert.False(_hasher.Verify("wrong-password-1", hash));
    }
}
