using EasyGameFramework.Api;

namespace EasyGameFramework;

public sealed class ConsoleLogger : ILogger
{
    public void Trace(string message)
    {
        Console.WriteLine(message);
    }

    public void Trace(object obj)
    {
        Trace(obj.ToString());
    }
}