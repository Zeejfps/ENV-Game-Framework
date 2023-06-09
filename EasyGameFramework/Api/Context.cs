using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public sealed class Context : IContext
{
    public Context(
        IWindow window,
        IInputSystem input)
    {
        Window = window;
        Input = input;
        Gpu = window.Gpu;
    }
    
    public IWindow Window { get; }
    public IInputSystem Input { get; }
    public IGpu Gpu { get; }
}