namespace ApiDiffSample;

public class Account
{
    public const int MaxOwners = 3;

    internal int AuditCount;

    public string Owner { get; set; } = "";

    public decimal Balance { get; set; }

    public void Deposit(decimal amount)
    {
        Balance += amount;
    }

    public void Transfer(Account to, decimal amount)
    {
        Balance -= amount;
        to.Deposit(amount);
    }

    public string Describe()
    {
        return Owner;
    }

    internal void Audit()
    {
        AuditCount++;
    }

    public class Statement
    {
        public int Lines { get; set; }
    }
}

public class Ledger
{
    public void Add(int amount)
    {
    }

    public void Add(long amount)
    {
    }
}

public class Notes
{
    public void Note(string? memo)
    {
    }
}

public interface IStore
{
    void Save(Account account);
}

public enum Currency
{
    Euro,
    Dollar,
    Pound,
}

[Obsolete("Use Account.Describe")]
public static class Legacy
{
    public static void Run()
    {
    }
}

internal class Helper
{
}
