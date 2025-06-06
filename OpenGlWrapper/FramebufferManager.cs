using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGlWrapper;

public sealed class FramebufferManager
{
    private readonly ShaderProgramManager m_ShaderProgramManager;
    private readonly VertexArrayObjectManager m_VertexArrayObjectManager;

    public FramebufferManager(ShaderProgramManager shaderProgramManager, VertexArrayObjectManager vertexArrayObjectManager)
    {
        m_VertexArrayObjectManager = vertexArrayObjectManager;
        m_ShaderProgramManager = shaderProgramManager;
    }

    public void Bind(FramebufferId framebufferId, FramebufferAccessFlag accessFlag = FramebufferAccessFlag.ReadWrite)
    {
        glBindFramebuffer((uint)accessFlag, framebufferId.Id);
        AssertNoGlError();
    }

    public void SetClearColor(float r, float g, float b, float a)
    {
        glClearColor(r, g, b, a);
        AssertNoGlError();
    }

    public void Clear(ClearFlags flags)
    {
        glClear((uint)flags);
        AssertNoGlError();
    }

    public void DrawArrayOfTriangles(ShaderProgramId shaderProgram, VertexArrayObjectId vao, int vertexCount, int skipCount = 0)
    {
        m_ShaderProgramManager.Bind(shaderProgram);
        m_VertexArrayObjectManager.Bind(vao);
        glDrawArrays(GL_TRIANGLES, skipCount, vertexCount);
        AssertNoGlError();
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        glViewport(x, y, width, height);
    }
}

public readonly struct FramebufferId : IEquatable<FramebufferId>
{
    public static FramebufferId WindowFramebuffer => new(0);
    
    internal uint Id { get; }

    public FramebufferId(uint id)
    {
        Id = id;
    }

    public bool Equals(FramebufferId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is FramebufferId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public static bool operator ==(FramebufferId left, FramebufferId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FramebufferId left, FramebufferId right)
    {
        return !left.Equals(right);
    }
}