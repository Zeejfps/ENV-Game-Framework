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
        
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
        
        fixed (float* ptr = &m_ProjectionMatrix.M11)
            glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
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
        if (m_Renderer.ItemCount > 0)
        {
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();
            
            m_Renderer.Render();
        }
    }
    
    public IRenderedPanel Render(Rect screenRect, PanelStyle style)
    {
        var p = new RenderedPanelImpl(this)
        {
            ScreenRect = screenRect,
            Style = style
        };
            
        m_Renderer.Add(p);
        return p;
    }

    internal void Destroy(RenderedPanelImpl panel)
    {
        m_Renderer.Remove(panel);
    }
}

sealed class RenderedPanelImpl : IRenderedPanel, IInstancedItem<Panel>
{
    public event Action<IInstancedItem<Panel>>? BecameDirty;
    
    private Rect m_ScreenRect;
    public Rect ScreenRect
    {
        get => m_ScreenRect;
        set
        {
            m_ScreenRect = value;
            BecameDirty?.Invoke(this);
        }
    }

    private PanelStyle m_Style;
    public PanelStyle Style
    {
        get => m_Style;
        set
        {
            m_Style = value;
            BecameDirty?.Invoke(this);
        }
    }

    private PanelRenderingSystem PanelRenderingSystem { get; }

    public RenderedPanelImpl(PanelRenderingSystem panelRenderingSystem)
    {
        PanelRenderingSystem = panelRenderingSystem;
    }

    public void Update(ref Panel panel)
    {
        var style = Style;
        panel.ScreenRect = ScreenRect;
        panel.BackgroundColor = style.BackgroundColor;
        panel.BorderRadius = style.BorderRadius;
        panel.BorderSize = style.BorderSize;
        panel.BorderColor = style.BorderColor;
    }

    private void ReleaseUnmanagedResources()
    {
        PanelRenderingSystem.Destroy(this);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RenderedPanelImpl()
    {
        ReleaseUnmanagedResources();
    }
}