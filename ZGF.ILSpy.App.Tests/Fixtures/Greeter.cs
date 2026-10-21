namespace ZGF.ILSpy.App.Tests.Fixtures;

public class Greeter
{
    public string Name = "";

    public string Greet(int times)
    {
        var text = "";
        for (var i = 0; i < times; i++)
            text += Name;
        return text;
    }

    public int Twice() => Greet(2).Length;
}
