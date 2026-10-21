namespace ZGF.ILSpy.App.Tests.Fixtures;

public class Farewell
{
    public string Say() => "Bye";

    public static bool IsLeapYear(int year) => false;
}

internal sealed class Hidden
{
    public int Secret() => 42;
}
