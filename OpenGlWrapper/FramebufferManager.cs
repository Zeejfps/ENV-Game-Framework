using static GL46;
using static OpenGlWrapper.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class FramebufferManager
{
    private readonly VertexArrayObjectManager m_VertexArrayObjectManager;

    public FramebufferManager(VertexArrayObjectManager vertexArrayObjectManager)
    {
        m_VertexArrayObjectManager = vertexArrayObjectManager;
    }

    public void SetClearColor(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
    }

    public void Clear(ClearFlags flags)
    {
        glClear((uint)flags);
    }

    public void DrawArrayOfTriangles(VertexArrayObjectHandle vao, int indicesCount)
    {
        m_VertexArrayObjectManager.Bind(vao);
        glDrawArrays(GL_TRIANGLES, 0, indicesCount);
        AssertNoGlError();
    }

    public void Bind(FramebufferId framebufferId)
    {
        
    }
}

public readonly struct FramebufferId
{
    public static FramebufferId WindowFramebuffer => new(0);
    
    internal uint Id { get; }

    public FramebufferId(uint id)
    {
        Id = id;
    }
}