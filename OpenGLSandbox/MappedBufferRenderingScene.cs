using System.Numerics;
using static OpenGL.Gl;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class MappedBufferRenderingScene : IScene
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
        var bufferSizeInBytes = vertexCount * sizeof(Vector3);
        glBufferData(GL_ARRAY_BUFFER, bufferSizeInBytes, IntPtr.Zero, GL_STATIC_DRAW);
        AssertNoGlError();

        var ptr = (void*)glMapBuffer(GL_ARRAY_BUFFER, GL_WRITE_ONLY);
        AssertNoGlError();

        var positionBuffer = new Span<Vector3>(ptr, vertexCount);
        positionBuffer[0] = new Vector3(0f, 0f, 0f); 
        positionBuffer[1] = new Vector3(0f, 0f, 0f);
        positionBuffer[2] = new Vector3(0f, 0f, 0f);

        glUnmapBuffer(GL_ARRAY_BUFFER);
        AssertNoGlError();

        var vertexShader = CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, "Assets/basic.vert.glsl");
        var fragmentShader = CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, "Assets/basic.frag.glsl");
    }

    public void Render()
    {
    }

    public void Unload()
    {
    }
}