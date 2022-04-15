using GLFW;

namespace SnakeGame;

public class GlfwApplication
{
    static GlfwApplication()
    {
        Glfw.Init();
    }
    
    public static GlfwApplicationBuilder CreateBuilder()
    {
        throw new NotImplementedException();
    }
}

public class GlfwApplicationBuilder
{
    public void UseOpenGL(int majorVersion, int minorVersion)
    {
        Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
        Glfw.WindowHint(Hint.ContextVersionMajor, majorVersion);
        Glfw.WindowHint(Hint.ContextVersionMinor, minorVersion);
        Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
    }

    public GlfwApplication Build()
    {
        return new GlfwApplication();
    }
}