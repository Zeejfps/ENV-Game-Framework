using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IApplication : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IGpu Gpu { get; }
    IGpuRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer);
    
    bool IsRunning { get; }
    
    void Update();
    void Quit();
}