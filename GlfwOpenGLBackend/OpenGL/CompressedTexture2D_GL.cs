using Framework;
using static OpenGL.Gl;

namespace TicTacToePrototype.OpenGL.AssetLoaders;

public class CompressedTexture2D_GL : ITexture
{
    public bool IsLoaded => m_Id != GL_NONE;
    public uint Id => m_Id;
    
    private uint m_Id;

    public unsafe CompressedTexture2D_GL(int width, int height, byte[]? pixels = null)
    {
        //Console.WriteLine($"Texture: {width}x{height}, pixels: {pixels.Length}");
        m_Id = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, m_Id);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);	
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

        if (pixels != null)
        {
            fixed (byte* p = &pixels[0])
                glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM_ARB, width, height, 0, width*height, p);
        }
       
        // fixed (byte* p = &pixels[0])
        //     glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, p);
        
        int err;
        while ((err = glGetError()) != GL_NO_ERROR)
        {
            Console.WriteLine($"GL ERROR: {err:X}");
        }
    }
    
    public void Unload()
    {
        glDeleteTexture(m_Id);
        m_Id = GL_NONE;
    }

    public void Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
    }
}