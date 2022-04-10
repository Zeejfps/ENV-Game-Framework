namespace ENV.Engine;

public interface IAsset
{
    bool IsLoaded { get; }

    void Unload();
}