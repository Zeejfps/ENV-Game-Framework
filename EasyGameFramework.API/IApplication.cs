using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IApplication : IDisposable
{
    IDisplays Displays { get; }
    IRenderer Renderer { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IGpu Gpu { get; }

    bool IsRunning { get; }
    
    void Update();
    void Quit();
}