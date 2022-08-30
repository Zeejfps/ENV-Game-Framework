namespace EasyGameFramework.API;

public sealed class Context : IContext
{
    public IDisplays Displays { get; }
    public IRenderer Renderer { get; }
    public IWindow Window { get; }
    public IInput Input { get; }
    public IGpu Gpu { get; }

    public Context(IDisplays displays, IRenderer renderer, IWindow window, IInput input, IGpu gpu)
    {
        Displays = displays;
        Renderer = renderer;
        Window = window;
        Input = input;
        Gpu = gpu;
    }
}