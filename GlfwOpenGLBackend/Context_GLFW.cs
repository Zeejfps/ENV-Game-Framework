using GLFW;
using GlfwOpenGLBackend.AssetLoaders;

namespace Framework.GLFW.NET;

public class Context_GLFW : IContext
{
    public IDisplays Displays { get; }
    public IWindow Window => m_Window;
    public IAssetDatabase AssetDatabase => m_AssetDatabase;

    private readonly Window_GLFW m_Window;
    private readonly AssetDatabase m_AssetDatabase;
    
    public Context_GLFW()
    {
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, 3);
        Glfw.WindowHint(Hint.ContextVersionMinor, 3);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
        Glfw.WindowHint(Hint.Doublebuffer, true);
        Glfw.WindowHint(Hint.Decorated, true);


        Displays = new Displays_GLFW();
        m_Window = new Window_GLFW();
        m_AssetDatabase = new AssetDatabase();
        
        m_AssetDatabase.AddLoader(new MeshAssetLoader_GL());
        m_AssetDatabase.AddLoader(new DebugMaterialAssetLoader_GL());
        m_AssetDatabase.AddLoader(new TextureAssetLoader_GL());
    }
    
    public IRenderbuffer CreateFramebuffer(int width, int height)
    {
        return new TextureFramebuffer_GL(width, height);
    }
    
    public void Dispose()
    {
        Glfw.Terminate();
    }
}