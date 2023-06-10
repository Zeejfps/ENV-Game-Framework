using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.Api;

public interface IGame
{
    IWindow Window { get; }
    IGpu Gpu { get; }
    IInputSystem Input { get; }
}