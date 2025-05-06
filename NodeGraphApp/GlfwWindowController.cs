using GLFW;
using WindowHandle = GLFW.Window;

namespace NodeGraphApp;

public sealed class GlfwWindowController
{
    private readonly WindowHandle _windowHandle;
    private readonly Window _window;

    private readonly SizeCallback _windowSizeCallback;
    
    public GlfwWindowController(WindowHandle windowHandle, Window window, Viewport viewport, OpenGlNodeGraphRenderer renderer)
    {
        _windowHandle = windowHandle;
        _window = window;

        _windowSizeCallback = (_, width, height) =>
        {
            Update();
            viewport.Update();
            renderer.Update();
            Glfw.SwapBuffers(windowHandle);
        };
        
        Glfw.SetWindowSizeCallback(windowHandle, _windowSizeCallback);
    }

    public void Update()
    {
        Glfw.GetFramebufferSize(_windowHandle, out var frameBufferWidth, out var frameBufferHeight);
        _window.FramebufferWidth = frameBufferWidth;
        _window.FramebufferHeight = frameBufferHeight;
        
        Glfw.GetWindowSize(_windowHandle, out var windowWidth, out var windowHeight);
        _window.Width = windowWidth;
        _window.Height = windowHeight;
    }
}