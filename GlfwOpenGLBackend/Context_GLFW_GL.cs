using Framework;
using Framework.GLFW.NET;
using GLFW;
using GlfwOpenGLBackend.AssetLoaders;

namespace GlfwOpenGLBackend;

public class Context_GLFW_GL : IContext
{
    public IDisplays Displays { get; }
    public IWindow Window => m_Window;
    public IInput Input => m_Window.Input;
    public ILocator Locator => m_Locator;

    private readonly Window_GLFW m_Window;
    private readonly ILocator m_Locator;
    
    public Context_GLFW_GL()
    {
        Glfw.Init();
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 3);
        Glfw.WindowHint(Hint.ContextVersionMinor, 3);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.Doublebuffer, true);
        Glfw.WindowHint(Hint.Decorated, true);

        Displays = new Displays_GLFW();
        m_Window = new Window_GLFW();
        m_Locator = new Locator();
        
        m_Locator.RegisterSingleton<IAssetLoader<IGpuMesh>>(new GpuMeshAssetLoader_GL());
        m_Locator.RegisterSingleton<IAssetLoader<IGpuShader>>(new GpuShaderAssetLoader_GL());
        m_Locator.RegisterSingleton<IAssetLoader<IGpuTexture>>(new GpuTextureAssetLoader_GL());
    }
    
    public IGpuRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        return new TextureFramebuffer_GL(width, height, colorBufferCount, createDepthBuffer);
    }
    
    public void Dispose()
    {
        Glfw.Terminate();
    }
}