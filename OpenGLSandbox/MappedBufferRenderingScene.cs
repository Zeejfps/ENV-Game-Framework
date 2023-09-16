using System.Numerics;
using static OpenGL.Gl;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class MappedBufferRenderingScene : IScene
{
    private const int VertexCount = 6;
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_ShaderProgram;
    
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

        var vertexCount = VertexCount;
        var bufferSizeInBytes = vertexCount * sizeof(Vector2);
        glBufferData(GL_ARRAY_BUFFER, bufferSizeInBytes, IntPtr.Zero, GL_STATIC_DRAW);
        AssertNoGlError();

        var ptr = (void*)glMapBuffer(GL_ARRAY_BUFFER, GL_WRITE_ONLY);
        AssertNoGlError();

        var positionBuffer = new Span<Vector2>(ptr, vertexCount);
        
        positionBuffer[0] = new Vector2(-0.90f, +0.85f); 
        positionBuffer[1] = new Vector2(+0.85f, -0.90f);
        positionBuffer[2] = new Vector2(-0.90f, -0.90f);
        
        positionBuffer[3] = new Vector2(+0.90f, +0.90f); 
        positionBuffer[4] = new Vector2(+0.90f, -0.85f);
        positionBuffer[5] = new Vector2(-0.85f, +0.90f);

        glUnmapBuffer(GL_ARRAY_BUFFER);
        AssertNoGlError();
        
        glVertexAttribPointer(0, 2, GL_FLOAT, false, sizeof(Vector2), IntPtr.Zero);
        glEnableVertexAttribArray(0);
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/basic.vert.glsl")
            .WithFragmentShader("Assets/basic.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
    }

    public void Render()
    {
        glDrawArrays(GL_TRIANGLES, 0, VertexCount);
    }

    public void Unload()
    {
    }
}