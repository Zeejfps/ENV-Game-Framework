using System.Numerics;
using OpenGL;

namespace OpenGLSandbox;

using static Gl;
using static Utils_GL;

public struct Triangle
{
    public Vertex V1;
    public Vertex V2;
    public Vertex V3;
}

public struct Vertex
{
    public Vector4 Color;
    public Vector2 Position;
}

public sealed unsafe class GouraudShadingRenderingScene : IScene
{
    private const int TriangleCount = 2;
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_ShaderProgram;
    
    public void Load()
    {
        m_Vao = glGenVertexArray();
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();
        
        m_Vbo = glGenBuffer();
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        AssertNoGlError();
        
        glBufferData(GL_ARRAY_BUFFER, TriangleCount * sizeof(Triangle), IntPtr.Zero, GL_STATIC_DRAW);
        AssertNoGlError();

        using (var buffer = new MappedBuffer<Triangle>(GL_ARRAY_BUFFER, TriangleCount))
        {
            buffer.Write(new Triangle
            {
                V1 =
                {
                    Color = new Vector4(0f, 0f, 1f, 1f),
                    Position = new Vector2(-0.90f, +0.85f),
                },
                V2 =
                {
                    Color = new Vector4(0f, 1f, 0f, 1f),
                    Position = new Vector2(+0.85f, -0.90f),
                },
                V3 =
                {
                    Color = new Vector4(1f, 0f, 0f, 1f),
                    Position = new Vector2(-0.90f, -0.90f),
                }
            });
            
            buffer.Write(new Triangle
            {
                V1 =
                {
                    Color = new Vector4(0.1f, 0.1f, 0.1f, 1f),
                    Position = new Vector2(+0.90f, +0.90f),
                },
                V2 =
                {
                    Color = new Vector4(0.5f, 0.5f, 0.5f, 1f),
                    Position = new Vector2(+0.90f, -0.85f),
                },
                V3 =
                {
                    Color = new Vector4(1f, 1f, 1f, 1f),
                    Position = new Vector2(-0.85f, +0.90f),
                }
            });
        }

        glVertexAttribPointer(0, 4, GL_FLOAT, false, sizeof(Vertex), (void*)0);
        glEnableVertexAttribArray(0);

        glVertexAttribPointer(1, 2, GL_FLOAT, false, sizeof(Vertex), (void*)sizeof(Vector4));
        glEnableVertexAttribArray(1);

        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/color.vert.glsl")
            .WithFragmentShader("Assets/color.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
    }

    public void Render()
    {
        glDrawArrays(GL_TRIANGLES, 0, TriangleCount * 3);
    }

    public void Unload()
    {
        glDeleteVertexArray(m_Vao);
        glDeleteBuffer(m_Vbo);
        glDeleteProgram(m_ShaderProgram);
    }
}