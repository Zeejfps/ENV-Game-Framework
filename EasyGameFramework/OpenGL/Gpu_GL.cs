using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

public class Gpu_GL : IGpu
{
    private readonly MeshManager_GL m_MeshManager;
    private readonly ShaderManager_GL m_ShaderManager;
    private readonly TextureManager_GL m_TextureManager;

    private bool m_EnableBackfaceCulling;

    private bool m_EnableBlending;
    private bool m_EnableDepthTest;

    public Gpu_GL(IWindow window)
    {
        m_MeshManager = new MeshManager_GL();
        m_TextureManager = new TextureManager_GL();
        m_ShaderManager = new ShaderManager_GL(m_TextureManager);
        Renderbuffer = new RenderbufferManager_GL(window, m_TextureManager);
    }

    public bool EnableDepthTest
    {
        get => m_EnableDepthTest;
        set
        {
            if (m_EnableBackfaceCulling == value)
                return;

            m_EnableDepthTest = value;
            if (m_EnableDepthTest)
                glEnable(GL_DEPTH_TEST);
            else
                glDisable(GL_DEPTH_TEST);
        }
    }

    public bool EnableBackfaceCulling
    {
        get => m_EnableBackfaceCulling;
        set
        {
            if (m_EnableBackfaceCulling == value)
                return;

            m_EnableBackfaceCulling = value;
            if (m_EnableBackfaceCulling)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);
        }
    }

    public bool EnableBlending
    {
        get => m_EnableBlending;
        set
        {
            if (m_EnableBlending == value)
                return;

            m_EnableBlending = value;
            if (m_EnableBlending)
            {
                glEnable(GL_BLEND);
                glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_DST_ALPHA);
            }
            else
            {
                glDisable(GL_BLEND);
            }
        }
    }

    public IMeshManager Mesh => m_MeshManager;
    public IShaderManager Shader => m_ShaderManager;
    public ITextureManager Texture => m_TextureManager;
    public IRenderbufferManager Renderbuffer { get; set; }

    private Stack<State> StateStack { get; } = new();

    public void SaveState()
    {
        StateStack.Push(new State
        {
            EnableBlending = EnableBlending,
            EnableBackfaceCulling = EnableBackfaceCulling,
            EnableDepthTest = EnableDepthTest
        });
    }

    public void RestoreState()
    {
        var state = StateStack.Pop();
        EnableBlending = state.EnableBlending;
        EnableBackfaceCulling = state.EnableBackfaceCulling;
        EnableDepthTest = state.EnableDepthTest;
    }

    private struct State
    {
        public bool EnableDepthTest { get; set; }
        public bool EnableBackfaceCulling { get; set; }
        public bool EnableBlending { get; set; }
    }
}