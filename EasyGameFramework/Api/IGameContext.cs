namespace EasyGameFramework.Api;

public interface IGameContext
{
    IDisplayManager DisplayManager { get; }
    IWindow Window { get; }
    ILogger Logger { get; }
}