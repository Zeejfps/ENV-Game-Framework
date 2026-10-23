namespace ZGF.ILSpy.App.Tests.Fixtures;

public class Farewell
{
    /// <summary>Says <c>Bye</c> to whoever is leaving.</summary>
    public string Say() => "Bye";

    public static bool IsLeapYear(int year) => false;

    public static string Compose(string greeting, string name, int repetitions, bool shout) => greeting + name + repetitions + shout;
}

internal sealed class Hidden
{
    public int Secret() => 42;
}
