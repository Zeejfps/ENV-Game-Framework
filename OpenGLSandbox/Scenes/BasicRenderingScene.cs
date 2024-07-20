using OpenGL;
using static OpenGL.Gl;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public sealed class BasicRenderingScene : IScene
{
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_ShaderProgram;
    
    public unsafe void Load()
    {
        var verts = new[]
        {
            -0.90f, +0.85f, 
            +0.85f, -0.90f, 
            -0.90f, -0.90f,
            
            +0.90f, +0.90f, 
            +0.90f, -0.85f,
            -0.85f, +0.90f,
        };
        
        m_Vao = glGenVertexArray();
        glBindVertexArray(m_Vao);
        
        m_Vbo = Gl.glGenBuffer();
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        fixed (float* ptr = &verts[0]) 
            glBufferData(GL_ARRAY_BUFFER, verts.Length * sizeof(float), ptr, GL_STATIC_DRAW);

        uint positionAttribIndex = 0;
        glVertexAttribPointer(positionAttribIndex, 2, GL_FLOAT, false, 2 * sizeof(float), IntPtr.Zero);
        glEnableVertexAttribArray(positionAttribIndex);

        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/basic.vert.glsl")
            .WithFragmentShader("Assets/basic.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        glClearColor(1f, 0f, 1f, 1f);
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArrays(GL_TRIANGLES, 0, 6);
        glFlush();
    }

    public void Unload()
    {
        glDeleteVertexArray(m_Vao);
        glDeleteBuffer(m_Vbo);
        glDeleteProgram(m_ShaderProgram);
    }
}