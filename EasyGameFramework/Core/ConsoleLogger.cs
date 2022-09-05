using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal sealed class ConsoleLogger : ILogger
{
    public void Trace(string message)
    {
        WriteLine("T", message);
    }

    public void Trace(object obj)
    {
        Trace(obj.ToString());
    }

    public void Warn(string message)
    {
        WriteLine("W", message);
    }

    private void WriteLine(string abrv, string message)
    {
        Console.WriteLine($@"[{DateTime.Now}] {abrv}: {message}");
    }
}