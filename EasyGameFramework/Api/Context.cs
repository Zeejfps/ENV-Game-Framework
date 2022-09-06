using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public sealed class Context : IContext
{
    public Context(IDisplays displays, IRenderer renderer, 
        IWindow window, IInputSystem input, 
        IGpu gpu, ILogger logger,
        IContainer container)
    {
        Displays = displays;
        Renderer = renderer;
        Window = window;
        Input = input;
        Gpu = gpu;
        Logger = logger;
        Container = container;
    }

    public IDisplays Displays { get; }
    public IRenderer Renderer { get; }
    public IWindow Window { get; }
    public IInputSystem Input { get; }
    public IGpu Gpu { get; }
    public ILogger Logger { get; }
    public IContainer Container { get; }
}