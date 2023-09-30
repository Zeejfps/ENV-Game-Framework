using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class PanelRenderingSystem : IPanelRenderingSystem
{     
    private readonly IWindow m_Window;
    
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;

    private readonly TexturedQuadInstancedRenderingSystem<Panel> m_Renderer;
    
    public PanelRenderingSystem(IWindow window)
    {
        m_Window = window;
        m_Renderer = new TexturedQuadInstancedRenderingSystem<Panel>(20000);
    }

    public void Load()
    {
        m_Renderer.Load();
        
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

    public void Unload()
    {
        m_Renderer.Unload();
       
        glUseProgram(0);
        AssertNoGlError();

        glDeleteProgram(m_ShaderProgram);
        AssertNoGlError();
        
        m_ShaderProgram = 0;
    }

    public void Update()
    {
        m_Renderer.Update();

        //Console.WriteLine($"Rendering: {m_Renderer.ItemCount}");
        // if (m_Renderer.ItemCount > 0)
        // {
        //     
        // }
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();

        fixed (float* ptr = &m_ProjectionMatrix.M11)
            glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
            
        m_Renderer.Render();
    }
    
    public IRenderedPanel Create(Rect screenRect, PanelStyle style)
    {
        var p = new RenderedPanelImpl
        {
            ScreenRect = screenRect,
            Style = style
        };
            
        m_Renderer.Add(p);
        return p;
    }
}