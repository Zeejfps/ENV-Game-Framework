using System.Numerics;
using static OpenGL.Gl;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class UIRectRenderingScene : IScene
{
    private const int InstanceCount = 1;
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

    struct PerInstanceAttribs
    {
        public Vector4 Color;
        public Vector4 BorderSize;
        public Vector4 BorderRadius;
    }
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_PerInstanceBuffer;
    private uint m_ShaderProgram;
    
    public void Load()
    {
        m_Vao = glGenVertexArray();
        AssertNoGlError();
        m_Vbo = glGenBuffer();
        AssertNoGlError();
        m_PerInstanceBuffer = glGenBuffer();
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, m_Vbo);
        AssertNoGlError();
        WriteVertexDataToBuffers();
        
        uint positionAttribIndex = 0;
        glVertexAttribPointer(positionAttribIndex, 2, GL_FLOAT, false, sizeof(Vertex), Offset(0));
        glEnableVertexAttribArray(positionAttribIndex);

        uint normalAttribIndex = 1;
        glVertexAttribPointer(normalAttribIndex, 2, GL_FLOAT, false, sizeof(Vertex), Offset(sizeof(Vector2)));
        glEnableVertexAttribArray(normalAttribIndex);
        
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        AssertNoGlError();
        using (var buffer = Buffer<PerInstanceAttribs>.Allocate(GL_ARRAY_BUFFER, InstanceCount, GL_STATIC_DRAW))
        {
            buffer.Write(new PerInstanceAttribs
            {
                Color = new Vector4(1f, 0f, 0f, 1f),
                BorderRadius = new Vector4(0f, 0f, 0f, 0f),
                BorderSize = new Vector4(0f, 0f, 0f, 0f)
            });
        }

        uint colorAttribIndex = 2;
        glVertexAttribPointer(colorAttribIndex, 4, GL_FLOAT, false, sizeof(PerInstanceAttribs), Offset(0));
        glEnableVertexAttribArray(colorAttribIndex);
        glVertexAttribDivisor(colorAttribIndex, 1);
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/uirect.vert.glsl")
            .WithFragmentShader("Assets/color.frag.glsl")
            .Build();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
    }

    private void WriteVertexDataToBuffers()
    {
        using (var buffer = Buffer<Triangle>.Allocate(GL_ARRAY_BUFFER, TriangleCount, GL_STATIC_DRAW))
        {
            buffer.Write(new Triangle
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
            
            buffer.Write(new Triangle
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
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArrays(GL_TRIANGLES, 0, TriangleCount * 3);
        glFlush();
    }

    public void Unload()
    {
        glDeleteVertexArray(m_Vao);
        glDeleteBuffer(m_Vbo);
        glDeleteBuffer(m_PerInstanceBuffer);
        glDeleteProgram(m_ShaderProgram);
    }
}