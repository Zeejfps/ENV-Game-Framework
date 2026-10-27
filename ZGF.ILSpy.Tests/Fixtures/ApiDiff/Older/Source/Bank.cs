namespace ApiDiffSample;

public class Account
{
    public const int MaxOwners = 2;

    public string Owner { get; set; } = "";

    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        Balance -= amount;
    }

    public virtual string Describe()
    {
        return Owner;
    }

    internal void Audit()
    {
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

    public void Add(string memo)
    {
    }
}

public class Notes
{
    public void Note(string memo)
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
}

public static class Legacy
{
    public static void Run()
    {
    }
}

public class Helper
{
}
