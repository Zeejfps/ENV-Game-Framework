using EasyGameFramework.Api;

namespace EasyGameFramework.Builder;

public sealed class GameContext : IContext
{
    public IDisplayManager DisplayManager { get; }
    public IWindow Window { get; }
    public ILogger Logger { get; }

    public GameContext(IDisplayManager displayManager, IWindow window, ILogger logger)
    {
        DisplayManager = displayManager;
        Logger = logger;
        Window = window;
    }
}