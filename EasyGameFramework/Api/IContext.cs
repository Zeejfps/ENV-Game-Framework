using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public interface IContext
{
    IWindow Window { get; }
    IInputSystem Input { get; }
    IGpu Gpu { get; }
}