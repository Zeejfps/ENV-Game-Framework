using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IContext : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IInput Input { get; }
    ILocator Locator { get; }
    IGpuRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer);
}