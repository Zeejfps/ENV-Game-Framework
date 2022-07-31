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
    public IAssetService AssetService => m_AssetService;

    private readonly Window_GLFW m_Window;
    private readonly AssetService m_AssetService;
    
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
        m_AssetService = new AssetService();
        
        m_AssetService.AddLoader(new MeshAssetLoader_GL());
        m_AssetService.AddLoader(new MaterialAssetLoader_GL());
        m_AssetService.AddLoader(new TextureAssetLoader_GL());
    }
    
    public IRenderbuffer CreateRenderbuffer(int width, int height, int colorBufferCount, bool createDepthBuffer)
    {
        return new TextureFramebuffer_GL(width, height, colorBufferCount, createDepthBuffer);
    }
    
    public void Dispose()
    {
        Glfw.Terminate();
    }
}