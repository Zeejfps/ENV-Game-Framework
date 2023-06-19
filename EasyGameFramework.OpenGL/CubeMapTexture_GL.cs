using EasyGameFramework.Api.AssetTypes;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class CubeMapTexture_GL : IGpuTexture, ITexture_GL
{
    public uint Id { get; }
    public int Width { get; }
    public int Height { get; }

    public CubeMapTexture_GL(uint id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }

    public bool IsLoaded { get; private set; }

    public void Dispose()
    {
    }

    public void Upload(ReadOnlySpan<byte> pixels, int? faceIndex)
    {
        switch (faceIndex)
        {
            case null:
                Console.Error.WriteLine($"Face index is null");
                return;
            case < 0 or >= 6:
                Console.Error.WriteLine($"Invalid face index: {faceIndex}");
                return;
        }

        glBindTexture(GL_TEXTURE_CUBE_MAP, Id);
        glAssertNoError();

        unsafe
        {
            fixed (void* p = &pixels[0])
            {
                glTexSubImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + faceIndex.Value, 0, 0, 0, Width, Height, GL_RGBA, GL_UNSIGNED_BYTE, p);
                glAssertNoError();
            }
        }
    }
}