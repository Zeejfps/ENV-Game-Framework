namespace Framework;

public interface IAsset
{
    bool IsLoaded { get; }

    void Unload();
}