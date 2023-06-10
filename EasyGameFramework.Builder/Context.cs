using EasyGameFramework.Api;
using EasyGameFramework.Glfw;

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

internal sealed class GlfwWindowFactory : IWindowFactory
{
    private ILogger Logger { get; }
    private IDisplayManager DisplayManager { get; }
    private IInputSystem InputSystem { get; }

    public GlfwWindowFactory(ILogger logger, IDisplayManager displayManager, IInputSystem inputSystem)
    {
        Logger = logger;
        DisplayManager = displayManager;
        InputSystem = inputSystem;
    }

    public IWindow Create()
    {
        return new Window_GLFW(Logger, DisplayManager, InputSystem);
    }
}