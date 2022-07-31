namespace Framework;

public interface IContext : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IAssetService AssetService { get; }
    IGpuRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer);
}