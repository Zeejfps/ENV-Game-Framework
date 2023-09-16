using System.Diagnostics;
using static OpenGL.Gl;

namespace OpenGLSandbox;

public sealed class InstanceRenderingScene : IScene
{
    private uint m_Vao;
    private uint m_Vbo;
    
    public unsafe void Load()
    {
        m_Vao = glGenVertexArray();
        AssertNoGlError();
        
        m_Vbo = glGenBuffer();
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        AssertNoGlError();

        var vertexCount = 3;
        var bufferSizeInBytes = vertexCount * 3 * sizeof(float);
        glBufferData(GL_ARRAY_BUFFER, bufferSizeInBytes, IntPtr.Zero, GL_STATIC_DRAW);
        AssertNoGlError();

        var ptr = (void*)glMapBuffer(GL_ARRAY_BUFFER, GL_WRITE_ONLY);
        AssertNoGlError();
        
        var buffer = new Span<float>(ptr, vertexCount * 3)
        {
            [0] = 1.0f, [1] = 1.0f, [2] = 1.0f,
            [3] = 0.0f, [4] = 0.0f, [5] = 2.0f,
            [6] = 0.0f, [7] = 0.0f, [8] = 0.0f
        };

        glUnmapBuffer(GL_ARRAY_BUFFER);
        AssertNoGlError();
    }

    public void Render()
    {
    }

    public void Unload()
    {
    }

    [Conditional("DEBUG")]
    private void AssertNoGlError()
    {
        Debug.Assert(!glTryGetError(out var error), error);
    }
}