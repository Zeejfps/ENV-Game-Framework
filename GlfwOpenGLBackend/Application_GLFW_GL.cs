using EasyGameFramework.API;
using EasyGameFramework.API.AssetTypes;
using EasyGameFramework.AssetManagement;
using Framework;
using Framework.GLFW.NET;
using GLFW;
using GlfwOpenGLBackend.AssetLoaders;

namespace GlfwOpenGLBackend;

public class Application_GLFW_GL : IApplication
{
    public IDisplays Displays { get; }
    public IWindow Window => m_Window;
    public IInput Input => m_Window.Input;
    public ILocator Locator => m_Locator;
    public IGpu Gpu { get; }
    public bool IsRunning { get; private set; }

    private readonly Window_GLFW m_Window;
    private readonly ILocator m_Locator;
    
    public Application_GLFW_GL()
    {
        Glfw.Init();
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 3);
        Glfw.WindowHint(Hint.ContextVersionMinor, 3);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.Doublebuffer, true);
        Glfw.WindowHint(Hint.Decorated, true);

        Gpu = new Gpu_GL();
        Displays = new Displays_GLFW();
        m_Window = new Window_GLFW(Displays);
        m_Locator = new Locator();

        m_Locator.RegisterSingleton<IAssetLoader<IGpuMesh>>(new GpuMeshAssetLoader_GL());
        m_Locator.RegisterSingleton<IAssetLoader<IGpuShader>>(new GpuShaderAssetLoader_GL());
        m_Locator.RegisterSingleton<IAssetLoader<IGpuTexture>>(new GpuTextureAssetLoader_GL());

        IsRunning = true;
    }
    
    public IGpuRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        return new TextureFramebuffer_GL(width, height, colorBufferCount, createDepthBuffer);
    }
    
    public void Update()
    {
        m_Window.Update();
        if (!m_Window.IsOpened)
            IsRunning = false;
    }

    public void Quit()
    {
        IsRunning = false;
    }

    public void Dispose()
    {
        Glfw.Terminate();
    }
}