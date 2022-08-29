using EasyGameFramework.API.AssetTypes;
using Framework;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend.OpenGL;

public class Texture2D_GL : IGpuTexture
{
    public bool IsLoaded { get; private set; }
    
    private uint m_Id;

    public Texture2D_GL(uint id)
    {
        m_Id = id;
    }
    
    public IGpuTextureHandle Use()
    {
        glBindTexture(GL_TEXTURE_2D, m_Id);
        return new Handle();
    }

    public void Dispose()
    {
        glDeleteTexture(m_Id);
        m_Id = 0;
        IsLoaded = false;
    }

    class Handle : IGpuTextureHandle
    {
        
    }
}