using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;



public unsafe class PanelRenderPass
{

    private uint m_Vao;
    private uint m_AttributesBuffer;
    private uint m_InstancesBuffer;
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    
    
    private const uint MaxPanelCount = 20000;
    private readonly IWindow m_Window;

    public PanelRenderPass(IWindow window)
    {
        m_Window = window;
    }

    public void Load()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        m_Vao = vao;

        Span<uint> buffers = stackalloc uint[2];
        fixed (uint* ptr = &buffers[0])
            glGenBuffers(buffers.Length, ptr);
        AssertNoGlError();

        m_AttributesBuffer = buffers[0];
        m_InstancesBuffer = buffers[1];
        
        glBindVertexArray(m_Vao);
        AssertNoGlError();
        
        SetupAttributesBuffer();
        SetupInstancesBuffer();
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/uirect.vert.glsl")
            .WithFragmentShader("Assets/uirect.frag.glsl")
            .Build();

        var bytes = Encoding.ASCII.GetBytes("projection_matrix");
        fixed(byte* ptr = &bytes[0])
            m_ProjectionMatrixUniformLocation = glGetUniformLocation(m_ShaderProgram, ptr);
        AssertNoGlError();

        var screenWidth = m_Window.ScreenWidth;
        var screenHeight = m_Window.ScreenHeight;
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, screenWidth, 0f, screenHeight, 0.1f, 100f);
    }
    
    public void Execute(ICommandBuffer commandBuffer)
    {
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();

        fixed (float* ptr = &m_ProjectionMatrix.M11)
            glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();

        var commands = commandBuffer.GetAllDrawPanelCommands();
        //Console.WriteLine("Commands: " + commands.Length);
        
        glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
        AssertNoGlError();
        // NOTE(Zee): Orphan the old buffer
        var bufferPtr = glMapBufferRange(
            GL_ARRAY_BUFFER, 
            IntPtr.Zero, 
            SizeOf<Panel>(commands.Length), 
            GL_MAP_WRITE_BIT | GL_MAP_INVALIDATE_BUFFER_BIT | GL_MAP_UNSYNCHRONIZED_BIT
        );
        AssertNoGlError();
        
        var buffer = new Span<Panel>(bufferPtr, commands.Length);
        for (var i = 0; i < commands.Length; i++)
        {
            var command = commands[i];
            buffer[i] = new Panel
            {
                Color = command.Color,
                BorderColor = command.BorderColor,
                BorderSize = command.BorderSize,
                BorderRadius = command.BorderRadius,
                ScreenRect = command.ScreenRect,
            };
        }
        
        glUnmapBuffer(GL_ARRAY_BUFFER);
        AssertNoGlError();
        
        glBindVertexArray(m_Vao);
        glDrawArraysInstanced(GL_TRIANGLES, 0, 6, commands.Length);
    }

    private void SetupAttributesBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_AttributesBuffer);
        AssertNoGlError();

        var texturedQuad = new TexturedQuad();
        glBufferData(GL_ARRAY_BUFFER, SizeOf<TexturedQuad>(), &texturedQuad, GL_STATIC_DRAW);
        AssertNoGlError();
        
        uint positionAttribIndex = 0;
        glVertexAttribPointer(
            positionAttribIndex, 
            2, 
            GL_FLOAT, 
            false, 
            sizeof(TexturedQuad.Vertex), 
            Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.Position))
        );
        glEnableVertexAttribArray(positionAttribIndex);

        uint normalAttribIndex = 1;
        glVertexAttribPointer(
            normalAttribIndex, 
            2, 
            GL_FLOAT,
            false, 
            sizeof(TexturedQuad.Vertex),
            Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords))
        );
        glEnableVertexAttribArray(normalAttribIndex);
    }
    
    private void SetupInstancesBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
        glBufferData(GL_ARRAY_BUFFER, SizeOf<Panel>(MaxPanelCount), (void*)0, GL_STREAM_DRAW);
        
        uint colorAttribIndex = 2;
        glVertexAttribPointer(
            colorAttribIndex, 
            4, 
            GL_FLOAT, 
            false, 
            sizeof(Panel), 
            Offset<Panel>(nameof(Panel.Color))
        );
        glEnableVertexAttribArray(colorAttribIndex);
        glVertexAttribDivisor(colorAttribIndex, 1);

        uint borderRadiusAttribIndex = 3;
        glVertexAttribPointer(
            borderRadiusAttribIndex, 
            4, GL_FLOAT,
            false, 
            sizeof(Panel), 
            Offset<Panel>(nameof(Panel.BorderRadius))
        );
        glEnableVertexAttribArray(borderRadiusAttribIndex);
        glVertexAttribDivisor(borderRadiusAttribIndex, 1);

        uint rectAttribIndex = 4;
        glVertexAttribPointer(
            rectAttribIndex, 
            4, 
            GL_FLOAT, 
            false, 
            sizeof(Panel), 
            Offset<Panel>(nameof(Panel.ScreenRect))
        );
        glEnableVertexAttribArray(rectAttribIndex);
        glVertexAttribDivisor(rectAttribIndex, 1);

        uint borderColorAttribIndex = 5;
        glVertexAttribPointer(
            borderColorAttribIndex, 
            4, GL_FLOAT, 
            false, 
            sizeof(Panel),
            Offset<Panel>(nameof(Panel.BorderColor))
        );
        glEnableVertexAttribArray(borderColorAttribIndex);
        glVertexAttribDivisor(borderColorAttribIndex, 1);
        
        uint borderSizeAttribIndex = 6;
        glVertexAttribPointer(
            borderSizeAttribIndex, 
            4, 
            GL_FLOAT, 
            false, 
            sizeof(Panel),
            Offset<Panel>(nameof(Panel.BorderSize))
        );
        glEnableVertexAttribArray(borderSizeAttribIndex);
        glVertexAttribDivisor(borderSizeAttribIndex, 1);
    }
}