using Framework;
using static OpenGL.Gl;

namespace TicTacToePrototype.OpenGL.AssetLoaders;

public class Texture2D_GL : ITexture
{
    public bool IsLoaded { get; private set; }

    public uint Id => m_Id;
    
    private uint m_Id;

    public Texture2D_GL(uint id)
    {
        m_Id = id;
    }
    
    public void Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
    }

    public void Resize(int width, int height)
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
        glTexImage2D(GL_TEXTURE2, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, IntPtr.Zero);
        glBindTexture(GL_TEXTURE_2D, 0);
    }

    public void Unload()
    {
        IsLoaded = false;
    }
}