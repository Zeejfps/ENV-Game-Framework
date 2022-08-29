using GlfwOpenGLBackend;

namespace EasyGameFramework.API;

public sealed class ApplicationBuilder
{
    public IContext Build()
    {
        var context = new Context_GLFW_GL();
        return context;
    }
}