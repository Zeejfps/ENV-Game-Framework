using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public interface IContext
{
    IDisplays Displays { get; }
    IRenderer Renderer { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IGpu Gpu { get; }
    ILogger Logger { get; }
    IContainer Container { get; }
}