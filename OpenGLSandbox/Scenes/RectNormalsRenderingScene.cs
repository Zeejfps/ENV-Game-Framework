using System.Numerics;
using static OpenGL.Gl;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public sealed unsafe class RectNormalsRenderingScene : IScene
{
    private const int TriangleCount = 2;
    
    struct Triangle
    {
        public Vertex V1;
        public Vertex V2;
        public Vertex V3;
    }

    struct Vertex
    {
        public Vector2 Position;
        public Vector2 UVs;
    }
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_ShaderProgram;
    
    public void Load()
    {
        m_Vao = glGenVertexArray();
        AssertNoGlError();
        m_Vbo = glGenBuffer();
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        AssertNoGlError();

        uint positionAttribIndex = 0;
        glVertexAttribPointer(positionAttribIndex, 2, GL_FLOAT, false, sizeof(Vertex), Offset(0));
        glEnableVertexAttribArray(positionAttribIndex);

        uint normalAttribIndex = 1;
        glVertexAttribPointer(normalAttribIndex, 2, GL_FLOAT, false, sizeof(Vertex), Offset(sizeof(Vector2)));
        glEnableVertexAttribArray(normalAttribIndex);
        
        using (var writer = BufferWriter<Triangle>.AllocateAndMap(GL_ARRAY_BUFFER, TriangleCount, GL_STATIC_DRAW))
        {
            writer.Write(new Triangle
            {
                V1 =
                {
                    Position = new Vector2(-0.5f, -0.5f),
                    UVs = new Vector2(0f, 0f)
                },
                V2 =
                {
                    Position = new Vector2(0.5f, -0.5f),
                    UVs = new Vector2(1f, 0f)
                },
                V3 =
                {
                    Position = new Vector2(-0.5f, 0.5f),
                    UVs = new Vector2(0f, 1f)
                }
            });
            
            writer.Write(new Triangle
            {
                V1 =
                {
                    Position = new Vector2(0.5f, -0.5f),
                    UVs = new Vector2(1f, 0f)
                },
                V2 =
                {
                    Position = new Vector2(0.5f, 0.5f),
                    UVs = new Vector2(1f, 1f)
                },
                V3 =
                {
                    Position = new Vector2(-0.5f, 0.5f),
                    UVs = new Vector2(0f, 1f)
                }
            });
        }

        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/normals.vert.glsl")
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
        glDeleteBuffer(m_Vbo);
        glDeleteProgram(m_ShaderProgram);
    }
}