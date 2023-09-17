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

        using (var buffer = new Buffer<Vector2>(GL_ARRAY_BUFFER, vertexCount))
        {
            buffer.Write(new Vector2(-0.90f, +0.85f));
            buffer.Write(new Vector2(+0.85f, -0.90f));
            buffer.Write(new Vector2(-0.90f, -0.90f));
            
            buffer.Write(new Vector2(+0.90f, +0.90f));
            buffer.Write(new Vector2(+0.90f, -0.85f));
            buffer.Write(new Vector2(-0.85f, +0.90f));
        }

        uint positionAttributeIndex = 0;
        glVertexAttribPointer(positionAttributeIndex, 2, GL_FLOAT, false, sizeof(Vector2), Offset(0));
        glEnableVertexAttribArray(positionAttributeIndex);
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/basic.vert.glsl")
            .WithFragmentShader("Assets/basic.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
        
        glClearColor(0f, 0f, 0f, 1f);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArrays(GL_TRIANGLES, 0, VertexCount);
        AssertNoGlError();
        glFlush();
    }

    public void Unload()
    {
        glDeleteProgram(m_ShaderProgram);
        glDeleteVertexArray(m_Vao);
        glDeleteBuffer(m_Vbo);
    }
}