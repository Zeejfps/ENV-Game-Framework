using System.Numerics;
using EasyGameFramework.Api;
using Entities;
using OpenGLSandbox;
using Tetris;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OOPEcs;

public sealed unsafe class SpriteRenderer : IEntity, ISpriteRenderer
{
    private readonly IWindow m_Window;
    private readonly TexturedQuadInstanceRenderer<Sprite> m_TexturedQuadInstanceRenderer;

    public SpriteRenderer(IWindow window)
    {
        m_Window = window;
        m_TexturedQuadInstanceRenderer = new TexturedQuadInstanceRenderer<Sprite>(100);
    }

    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    
    public IRenderedSprite Render(Rect screenRect)
    {
        var renderedSprite = new RenderedSpriteImpl(screenRect, this);
        m_TexturedQuadInstanceRenderer.Add(renderedSprite);
        return renderedSprite;
    }

    internal void Destroy(RenderedSpriteImpl renderedSpriteImpl)
    {
        m_TexturedQuadInstanceRenderer.Remove(renderedSpriteImpl);
    }

    public void Load()
    {
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/shaders/basic.vert.glsl")
            .WithFragmentShader("Assets/shaders/basic.frag.glsl")
            .Build();
        
        m_ProjectionMatrixUniformLocation = GetUniformLocation(m_ShaderProgram, "u_ProjectionMatrix");
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);

        m_TexturedQuadInstanceRenderer.Load();
    }

    public void Unload()
    {
        m_TexturedQuadInstanceRenderer.Unload();
    }

    public void Update()
    {
        glUseProgram(m_ShaderProgram);
        
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);
        fixed (float* ptr = &m_ProjectionMatrix.M11)
            glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        m_TexturedQuadInstanceRenderer.Update();
        m_TexturedQuadInstanceRenderer.Render();
    }
}

sealed class RenderedSpriteImpl : IRenderedSprite, IInstancedItem<Sprite>
{
    private Rect m_ScreenRect;
    public Rect ScreenRect
    {
        get => m_ScreenRect;
        set
        {
            if (m_ScreenRect.Equals(value))
                return;
            m_ScreenRect = value;
            BecameDirty?.Invoke(this);
        }
    }

    private readonly SpriteRenderer m_SpriteRenderer;
    
    public RenderedSpriteImpl(Rect screenRect, SpriteRenderer spriteRenderer)
    {
        ScreenRect = screenRect;
        m_SpriteRenderer = spriteRenderer;
    }
    
    public event Action<IInstancedItem<Sprite>>? BecameDirty;
    
    public void Update(ref Sprite instancedData)
    {
        instancedData.ScreenRect = ScreenRect;
    }

    public void Dispose()
    {
        m_SpriteRenderer.Destroy(this);
    }
}

public struct Sprite
{
    [InstancedAttrib(4, GL46.GL_FLOAT)]
    public Rect ScreenRect;
}