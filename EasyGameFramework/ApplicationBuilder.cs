using GlfwOpenGLBackend;

namespace EasyGameFramework.API;

public sealed class ApplicationBuilder
{
    public IApplication Build()
    {
        var app = new Application_GLFW_GL();
        return app;
    }
}