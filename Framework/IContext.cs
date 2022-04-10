namespace ENV.Engine;

public interface IContext : IDisposable
{
    IDisplays Displays { get; }
    IWindow Window { get; }
    IAssetDatabase AssetLoader { get; }
}