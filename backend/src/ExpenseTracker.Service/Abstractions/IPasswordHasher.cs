namespace ExpenseTracker.Service.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
