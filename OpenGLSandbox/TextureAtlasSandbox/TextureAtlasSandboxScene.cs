using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox.TextureAtlasSandbox;

public sealed unsafe class TextureAtlasSandboxScene : IScene
{
    private uint m_TextureId;
    
    public void Load()
    {
        uint id;
        glGenTextures(1, &id);
        AssertNoGlError();
        m_TextureId = id;
        
        glBindTexture(GL_TEXTURE_2D_ARRAY, m_TextureId);
        AssertNoGlError();
        
        //glTexImage3D();
    }

    public void Unload()
    {
        fixed (uint* ptr = &m_TextureId)
            glDeleteTextures(1, ptr);
        AssertNoGlError();
    }

    public void Update()
    {

    }
}