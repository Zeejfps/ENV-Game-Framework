namespace Framework;

public interface IMesh : IAsset
{
    IMeshApi Use();
}

public interface IMeshApi : IDisposable
{
    void Render();
}