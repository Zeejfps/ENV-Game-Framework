namespace Framework;

public interface IContext : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IAssetDatabase AssetDatabase { get; }
    IFramebuffer CreateFramebuffer(int width, int height);
}