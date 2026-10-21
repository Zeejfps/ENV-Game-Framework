using System.Text;

namespace ZGF.ILSpy.App.Tests.Fixtures;

public class Outline
{
    private readonly List<int> _values = new();

    public int First()
    {
        var total = 0;
        foreach (var value in _values)
            total += value;
        return total;
    }

    public string Second()
    {
        var builder = new StringBuilder();
        builder.Append("needle");
        return builder.ToString();
    }

    public int Third() => First();

    public class Inner
    {
        public int Deep()
        {
            return 7;
        }
    }
}
