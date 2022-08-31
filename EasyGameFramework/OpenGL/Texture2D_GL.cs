using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.OpenGL;

public class Texture2D_GL : IGpuTexture
{
    public Texture2D_GL(uint id)
    {
        Id = id;
    }

    public bool IsLoaded { get; private set; }
    public uint Id { get; }

    public void Dispose()
    {
    }
}