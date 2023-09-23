using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class SimpleVao
{
    private uint m_VertexArrayObjectId;
    private uint m_VertexAttributesBufferId;
    private uint m_ShaderProgramId;
    
    public void Load()
    {
        m_VertexArrayObjectId = glGenVertexArray();
        AssertNoGlError();

        m_VertexAttributesBufferId = glGenBuffer();
        AssertNoGlError();
        
        glBindVertexArray(m_VertexArrayObjectId);
        AssertNoGlError();
        glBindBuffer(GL_ARRAY_BUFFER, m_VertexAttributesBufferId);
        AssertNoGlError();
    }

    private uint glGenVertexArray()
    {
        uint id;
        glGenVertexArrays(1, &id);
        return id;
    }

    private uint glGenBuffer()
    {
        uint id;
        glGenBuffers(1, &id);
        return id;
    }
}