namespace EasyGameFramework.Api;

public interface IContext
{
    IDisplayManager DisplayManager { get; }
    IWindow Window { get; }
    ILogger Logger { get; }
}