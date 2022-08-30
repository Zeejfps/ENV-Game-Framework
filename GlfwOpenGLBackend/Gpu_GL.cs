using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework.GLFW.NET;
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

    public IMeshManager Mesh => m_MeshManager;
    public IShaderManager Shader => m_ShaderManager;
    public ITextureManager Texture => m_TextureManager;
    public IRenderbufferManager Renderbuffer { get; set; }

    private readonly MeshManager_GL m_MeshManager;
    private readonly ShaderManager_GL m_ShaderManager;
    private readonly TextureManager_GL m_TextureManager;

    public Gpu_GL(IWindow window)
    {
        m_MeshManager = new MeshManager_GL();
        m_ShaderManager = new ShaderManager_GL();
        m_TextureManager = new TextureManager_GL();
        Renderbuffer = new RenderbufferManager_GL(window, m_TextureManager);
    }

    public IHandle<IGpuMesh> LoadMesh(string assetPath)
    {
        var cpuMesh = m_CpuMeshLoader.Load(assetPath);
        var gpuMesh = new Mesh_GL(cpuMesh.Vertices, cpuMesh.Normals, cpuMesh.Uvs, cpuMesh.Tangents,
            cpuMesh.Triangles);
        var handle = new GpuMeshHandle(gpuMesh);
        m_MeshManager.Add(handle, gpuMesh);
        return handle;
    }

    public IHandle<IGpuShader> LoadShader(string assetPath)
    {
        var cpuShader = m_CpuShaderLoader.Load(assetPath);
        var gpuShader = Shader_GL.LoadFromSource(cpuShader.VertexShader, cpuShader.FragmentShader, Texture);
        var handle = new GpuShaderHandle(gpuShader);
        m_ShaderManager.Add(handle, gpuShader);
        return handle;
    }

    public IHandle<IGpuTexture> LoadTexture(string assetPath)
    {
        var asset = m_CpuTextureAssetLoader.Load(assetPath);
        var width = asset.Width;
        var height = asset.Height;
        var pixels = asset.Pixels;
        var texture = ReadonlyTexture2D_GL.Create(width, height, pixels);
        var handle = new GpuReadonlyTextureHandle(texture);
        m_TextureManager.Add(handle, texture);
        return handle;
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