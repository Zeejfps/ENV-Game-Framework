using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.OpenGL;

public class Texture2D_GL : IGpuTexture
{
    public int Width { get; }
    
    public int Height { get; }
    
    public Texture2D_GL(uint id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }

    public bool IsLoaded { get; private set; }
    public uint Id { get; }

    public void Dispose()
    {
    }
}