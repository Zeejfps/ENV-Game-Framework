using ENV.Engine;
using GLFW;
using TicTacToePrototype.OpenGL.AssetLoaders;

namespace ENV.GLFW.NET;

public class Context_GLFW : IContext
{
    public IDisplays Displays { get; }
    public IWindow Window => m_Window;
    public IAssetLoader AssetLoader => m_ModularAssetLoader;

    private readonly Window_GLFW m_Window;
    private readonly ModularAssetLoader m_ModularAssetLoader;
    
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
        m_ModularAssetLoader = new ModularAssetLoader();
        
        m_ModularAssetLoader.AddModule(new MeshAssetLoaderModule());
        m_ModularAssetLoader.AddModule(new MaterialAssetLoaderModule());
        m_ModularAssetLoader.AddModule(new TextureAssetLoader_GL());
    }
    
    public void Dispose()
    {
        Glfw.Terminate();
    }
}