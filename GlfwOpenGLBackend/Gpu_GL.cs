using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

public class Gpu_GL : IGpu
{
    private bool m_EnableDepthTest;
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

    private bool m_EnableBackfaceCulling;
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

    private bool m_EnableBlending;
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

    public IHandle<IGpuShader> LoadShader(string shaderPath)
    {
        var cpuShaderLoader = new CpuShaderAssetLoader();
        var cpuShader = cpuShaderLoader.Load(shaderPath);
        return new GpuShaderHandle(Shader_GL.LoadFromSource(cpuShader.VertexShader, cpuShader.FragmentShader));
    }

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

    private Stack<State> StateStack { get; } = new();

    struct State
    {
        public bool EnableDepthTest { get; set; }
        public bool EnableBackfaceCulling { get; set; }
        public bool EnableBlending { get; set; }
    }
}