using System.Numerics;
using static OpenGL.Gl;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

[AttributeUsage(AttributeTargets.Field)]
public sealed class InstancedAttrib : Attribute
{
    public InstancedAttrib(uint componentCount, uint componentType)
    {
        ComponentCount = (int)componentCount;
        ComponentType = (int)componentType;
    }

    public int ComponentCount { get; }
    public int ComponentType { get; }
}

public struct Panel
{
    [InstancedAttrib(4, GL_FLOAT)]
    public Rect ScreenRect;
    [InstancedAttrib(4, GL_FLOAT)]
    public Color BackgroundColor;
    [InstancedAttrib(4, GL_FLOAT)]
    public Color BorderColor;
    [InstancedAttrib(4, GL_FLOAT)]
    public BorderSize BorderSize;
    [InstancedAttrib(4, GL_FLOAT)]
    public Vector4 BorderRadius;
}

public sealed unsafe class WidgetRectRenderingScene : IScene
{
    private const int InstanceCount = 2;
    private const int TriangleCount = 2;
    
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
        glVertexAttribPointer(positionAttribIndex, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset(0));
        glEnableVertexAttribArray(positionAttribIndex);

        uint normalAttribIndex = 1;
        glVertexAttribPointer(normalAttribIndex, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset(sizeof(Vector2)));
        glEnableVertexAttribArray(normalAttribIndex);
        
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        AssertNoGlError();
        using (var buffer = BufferWriter<Panel>.AllocateAndMap(GL_ARRAY_BUFFER, InstanceCount, GL_STATIC_DRAW))
        {
            buffer.Write(new Panel
            {
                BackgroundColor = new Color(0f, 0.5f, 0.6f, 1f),
                BorderColor = new Color(1f, 0f, 0.6f, 1f),
                BorderSize = BorderSize.FromTRBL(60f, 00f, 00f, 10f),
                BorderRadius = new Vector4(80f, 50f, 0f, 50f),
                ScreenRect = new Rect(100f, 100f, 500f, 300f)
            });
            buffer.Write(new Panel
            {
                BackgroundColor = new Color(1.0f, 0f, 1.0f, 1f),
                BorderColor = new Color(0f,0.3f, 1f, 1f),
                BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                BorderSize = BorderSize.FromTRBL(5f, 5f, 5f, 5f),
                ScreenRect = new Rect(10f, 10f, 200f, 100f)
            });
        }

        uint colorAttribIndex = 2;
        glVertexAttribPointer(colorAttribIndex, 4, GL_FLOAT, false, sizeof(Panel), 
            Offset<Panel>(nameof(Panel.BackgroundColor)));
        glEnableVertexAttribArray(colorAttribIndex);
        glVertexAttribDivisor(colorAttribIndex, 1);

        uint borderRadiusAttribIndex = 3;
        glVertexAttribPointer(borderRadiusAttribIndex, 4, GL_FLOAT, false, sizeof(Panel), 
            Offset<Panel>(nameof(Panel.BorderRadius)));
        glEnableVertexAttribArray(borderRadiusAttribIndex);
        glVertexAttribDivisor(borderRadiusAttribIndex, 1);

        uint rectAttribIndex = 4;
        glVertexAttribPointer(rectAttribIndex, 4, GL_FLOAT, false, sizeof(Panel), 
            Offset<Panel>(nameof(Panel.ScreenRect)));
        glEnableVertexAttribArray(rectAttribIndex);
        glVertexAttribDivisor(rectAttribIndex, 1);

        uint borderColorAttribIndex = 5;
        glVertexAttribPointer(borderColorAttribIndex, 4, GL_FLOAT, false, sizeof(Panel),
            Offset<Panel>(nameof(Panel.BorderColor)));
        glEnableVertexAttribArray(borderColorAttribIndex);
        glVertexAttribDivisor(borderColorAttribIndex, 1);
        
        uint borderSizeAttribIndex = 6;
        glVertexAttribPointer(borderSizeAttribIndex, 4, GL_FLOAT, false, sizeof(Panel),
            Offset<Panel>(nameof(Panel.BorderSize)));
        glEnableVertexAttribArray(borderSizeAttribIndex);
        glVertexAttribDivisor(borderSizeAttribIndex, 1);
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/uirect.vert.glsl")
            .WithFragmentShader("Assets/uirect.frag.glsl")
            .Build();

        var projectionMatrixUniformLocation = glGetUniformLocation(m_ShaderProgram, "projection_matrix");
        AssertNoGlError();
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
        
        var projection = Matrix4x4.CreateOrthographicOffCenter(0f, 640f, 0f, 640f, 0.1f, 100f);
        float* ptr = &projection.M11;
        glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        glClearColor(0.2f, 0.1f, 0.7f, 1f);
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);  
    }  

    private void WriteVertexDataToBuffers()
    {
        using (var buffer = BufferWriter<TexturedQuad>.AllocateAndMap(GL_ARRAY_BUFFER, TriangleCount, GL_STATIC_DRAW))
        {
            buffer.Write(new TexturedQuad());
        }
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        glDrawArraysInstanced(GL_TRIANGLES, 0, TriangleCount * 3, InstanceCount);
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