namespace Framework;

public interface IContext : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IInput Input { get; }
    IAssetDatabase AssetDatabase { get; }
    IRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer);
}