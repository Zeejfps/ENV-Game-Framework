using EasyGameFramework.Api;

namespace EasyGameFramework.Builder;

public sealed class GameGameContext : IGameContext
{
    public IDisplayManager DisplayManager { get; }
    public IWindow Window { get; }
    public ILogger Logger { get; }

    public GameGameContext(IDisplayManager displayManager, IWindow window, ILogger logger)
    {
        DisplayManager = displayManager;
        Logger = logger;
        Window = window;
    }
}