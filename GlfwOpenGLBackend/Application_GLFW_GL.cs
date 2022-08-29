using EasyGameFramework.API;
using GLFW;

namespace GlfwOpenGLBackend;

public class Application_GLFW_GL : IApplication
{
    public IDisplays Displays { get; }
    public IRenderer Renderer { get; }
    public IWindow Window { get; set; }
    public IInput Input { get; set; }
    public IGpu Gpu { get; }
    public bool IsRunning { get; private set; }

    public Application_GLFW_GL(IDisplays displays, IWindow window, IInput input, IGpu gpu, IRenderer renderer)
    {
        Displays = displays;
        Window = window;
        Input = input;
        Gpu = gpu;
        Renderer = renderer;

        IsRunning = true;
    }

    public void Update()
    {
        // Order matters here
        Input.Update();
        Window.Update();
        if (!Window.IsOpened)
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