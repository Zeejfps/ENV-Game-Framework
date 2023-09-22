using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;


enum BufferId
{
    Vbo,
    Count
}

public unsafe class BasicTextureRenderingScene : IScene
{
    private uint m_BufferId;
    private uint m_TextureId;
    
    public void Load()
    {
        uint bufferId;
        glGenBuffers(1, &bufferId);
        AssertNoGlError();

        m_BufferId = bufferId;

        uint textureId;
        glGenTextures(1, &textureId);
        AssertNoGlError();

        m_TextureId = textureId;
        
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();
        
        glTexStorage2D(GL_TEXTURE_2D, 1, GL_RGBA8, 10, 10);
        AssertNoGlError();
    }

    public void Render()
    {
    }

    public void Unload()
    {
        fixed (uint* ptr = &m_BufferId)
            glDeleteBuffers(1, ptr);
        
        fixed (uint* ptr = &m_TextureId)
            glDeleteTextures(1, ptr);
    }
}