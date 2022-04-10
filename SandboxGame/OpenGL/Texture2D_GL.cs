using ENV.Engine;
using static OpenGL.Gl;

namespace TicTacToePrototype.OpenGL.AssetLoaders;

public class Texture2D_GL : ITexture
{
    public bool IsLoaded => m_Id != GL_NONE;

    private uint m_Id;
    
    public unsafe Texture2D_GL(int width, int height, byte[] pixels)
    {
        Console.WriteLine($"Texture: {width}x{height}, pixels: {pixels.Length}");
        
        m_Id = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, m_Id);

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);	
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        
        fixed (byte* p = &pixels[0])
            glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM_ARB, width, height, 0, width*height, p);

        // fixed (byte* p = &pixels[0])
        //     glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, p);
        
        int err;
        while ((err = GetError()) != GL_NO_ERROR)
        {
            Console.WriteLine($"GL ERROR: {err}");
        }
        
       
    }
    
    public void Unload()
    {
        glDeleteTexture(m_Id);
        m_Id = GL_NONE;
    }

    public void Use()
    {
        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D, m_Id);
        glActiveTexture(GL_TEXTURE0);
    }
}