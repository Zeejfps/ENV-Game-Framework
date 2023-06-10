using EasyGameFramework.Api;
using EasyGameFramework.Glfw;

namespace EasyGameFramework.Builder;

public sealed class Backend : IBackend
{
    public IDisplayManager DisplayManager { get; }
    public IWindowFactory WindowFactory { get; }
    public ILogger Logger { get; }

    public Backend(IDisplayManager displayManager, IWindowFactory windowFactory, ILogger logger)
    {
        DisplayManager = displayManager;
        WindowFactory = windowFactory;
        Logger = logger;
    }
}

internal sealed class GlfwWindowFactory : IWindowFactory
{
    private ILogger Logger { get; }
    private IDisplayManager DisplayManager { get; }
    private IInputSystem InputSystem;

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