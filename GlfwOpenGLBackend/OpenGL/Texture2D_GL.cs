using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.OpenGL;

public class Texture2D_GL : ITexture
{
    public bool IsLoaded { get; private set; }
    
    private uint m_Id;

    public Texture2D_GL(uint id)
    {
        m_Id = id;
    }
    
    public void Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
    }

    public void Unload()
    {
        glDeleteTexture(m_Id);
        m_Id = 0;
        IsLoaded = false;
    }
}