namespace EasyGameFramework.API;

public interface IContext
{
    IDisplays Displays { get; }
    IRenderer Renderer { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IGpu Gpu { get; }
    ILogger Logger { get; }
}