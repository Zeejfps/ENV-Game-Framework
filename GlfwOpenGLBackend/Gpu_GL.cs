using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework.GLFW.NET;
using GlfwOpenGLBackend.OpenGL;
using TicTacToePrototype.OpenGL.AssetLoaders;
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

    private readonly CpuMeshAssetLoader m_CpuMeshLoader = new();
    private readonly CpuShaderAssetLoader m_CpuShaderLoader = new();
    private readonly CpuTextureAssetLoader m_CpuTextureAssetLoader = new();
    
    public IHandle<IGpuRenderbuffer> CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        return new GpuTextureFramebufferHandle(new TextureFramebuffer_GL(width, height, colorBufferCount, createDepthBuffer));
    }
    
    public IHandle<IGpuMesh> LoadMesh(string assetPath)
    {
        var cpuMesh = m_CpuMeshLoader.Load(assetPath);
        return new GpuMeshHandle(new Mesh_GL(cpuMesh.Vertices, cpuMesh.Normals, cpuMesh.Uvs, cpuMesh.Tangents,
            cpuMesh.Triangles));
    }

    public IHandle<IGpuShader> LoadShader(string assetPath)
    {
        var cpuShader = m_CpuShaderLoader.Load(assetPath);
        return new GpuShaderHandle(Shader_GL.LoadFromSource(cpuShader.VertexShader, cpuShader.FragmentShader));
    }

    public IHandle<IGpuTexture> LoadTexture(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        return new GpuReadonlyTextureHandle(new ReadonlyTexture2D_GL(width, height, pixels));
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