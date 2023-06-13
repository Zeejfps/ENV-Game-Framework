using EasyGameFramework.Api;

namespace EasyGameFramework.Builder;

public sealed class Context : IContext
{
    public IDisplayManager DisplayManager { get; }
    public IWindow Window { get; }
    public ILogger Logger { get; }

    public Context(IDisplayManager displayManager, IWindow window, ILogger logger)
    {
        DisplayManager = displayManager;
        Logger = logger;
        Window = window;
    }
}