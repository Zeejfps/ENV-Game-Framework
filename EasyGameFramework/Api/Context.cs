using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public sealed class Context : IContext
{
    public Context(
        IWindow window,
        IInputSystem input, 
        ILogger logger,
        IContainer container)
    {
        Window = window;
        Input = input;
        Gpu = window.Gpu;
        Logger = logger;
        Container = container;
    }
    
    public IWindow Window { get; }
    public IInputSystem Input { get; }
    public IGpu Gpu { get; }
    public ILogger Logger { get; }
    public IContainer Container { get; }
}