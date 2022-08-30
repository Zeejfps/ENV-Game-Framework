namespace EasyGameFramework.API;

public interface ILogger
{
    void Trace(string message);
    void Trace(object obj);
}