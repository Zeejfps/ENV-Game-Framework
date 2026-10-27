namespace ZGF.ILSpy.Tests.Fixtures;

public interface IFeeds
{
    void Feed();
}

public abstract class Animal
{
    private int _age = 1;
    protected string Tag = "";

    public string Name { get; set; } = "";

    public Keeper? Keeper { get; set; }

    public abstract void Speak();

    public virtual string Describe() => Name + _age;

    public override string ToString() => Name;

    internal void Mark()
    {
    }

    public static Animal? Loudest() => null;
}

public class Lion : Animal, IFeeds
{
    public int Roars { get; set; }

    public override void Speak()
    {
    }

    public void Feed()
    {
    }
}

public sealed class Parrot : Animal
{
    public override void Speak()
    {
    }
}

public class Keeper
{
    public List<Animal> Animals { get; } = new();

    public Enclosure Home { get; set; }

    public event Action? Fed;

    public void Feed() => Fed?.Invoke();
}

public struct Enclosure
{
    public int Size;
}

public static class Visits
{
    public static int Count;
}
