using EasyGameFramework.Api;

namespace EasyGameFramework;

public sealed class Context : IContext
{
    public Context(IDisplays displays, IRenderer renderer, IWindow window, IInput input, IGpu gpu, ILogger logger)
    {
        Displays = displays;
        Renderer = renderer;
        Window = window;
        Input = input;
        Gpu = gpu;
        Logger = logger;
    }

    public IDisplays Displays { get; }
    public IRenderer Renderer { get; }
    public IWindow Window { get; }
    public IInput Input { get; }
    public IGpu Gpu { get; }
    public ILogger Logger { get; }
}