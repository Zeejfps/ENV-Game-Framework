using EasyGameFramework.Api.AssetTypes;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class Texture2D_GL : IGpuTexture
{
    public uint Id { get; }
    public int Width { get; }
    public int Height { get; }

    public Texture2D_GL(uint id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }

    public bool IsLoaded { get; private set; }

    public void Dispose()
    {
    }

    public void Upload(ReadOnlySpan<byte> pixels)
    {
        glBindTexture(GL_TEXTURE_2D, Id);
        glAssertNoError();

        unsafe
        {
            fixed (void* p = &pixels[0])
            {
                glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, Width, Height, GL_RGBA, GL_UNSIGNED_BYTE, p);
                glAssertNoError();
            }
        }
        
    }
}