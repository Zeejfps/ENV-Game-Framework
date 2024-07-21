using System.Numerics;
using OpenGL;

namespace OpenGLSandbox;

using static Gl;
using static OpenGlUtils;

public sealed unsafe class GouraudShadingRenderingScene : IScene
{
    struct Triangle
    {
        public Vertex V1;
        public Vertex V2;
        public Vertex V3;
    }

    struct Vertex
    {
        public Vector4 Color;
        public Vector2 Position;
    }
    
    private const int TriangleCount = 2;
    
    private uint m_Vao;
    private uint m_ShaderProgram;
    
    private ArrayBuffer<Triangle> m_Vbo = new();
    
    public void Load()
    {
        m_Vao = glGenVertexArray();
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();

        m_Vbo.Alloc(TriangleCount, ArrayBufferUsageHint.StaticDraw);
        m_Vbo.WriteMapped(0, TriangleCount, memory =>
        {
            var data = memory.Span;
            data[0] = new Triangle
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
            };
            data[1] = new Triangle
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
            };
        });

        glVertexAttribPointer(0, 4, GL_FLOAT, false, sizeof(Vertex), Offset(0));
        glEnableVertexAttribArray(0);

        glVertexAttribPointer(1, 2, GL_FLOAT, false, sizeof(Vertex), Offset(sizeof(Vector4)));
        glEnableVertexAttribArray(1);

        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/color.vert.glsl")
            .WithFragmentShader("Assets/color.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArrays(GL_TRIANGLES, 0, TriangleCount * 3);
        glFlush();
    }

    public void Unload()
    {
        glDeleteVertexArray(m_Vao);
        glDeleteProgram(m_ShaderProgram);
        m_Vbo.Free();
    }
}