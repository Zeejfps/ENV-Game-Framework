using static GL46;
using GLFW;
using ZGF.GlfwUtils;

namespace SoftwareRendererOpenGlBackend;

public sealed class QuadTreeRendererApp : OpenGlApp
{
    private readonly QuadTreeRenderer _renderer;
    
    public QuadTreeRendererApp(StartupConfig startupConfig) : base(startupConfig)
    {
        _renderer = new QuadTreeRenderer();

        Glfw.SetFramebufferSizeCallback(WindowHandle, HandleFrameBufferSizeEvent);
        Glfw.SetMouseButtonCallback(WindowHandle, HandleMouseButtonEvent);
        Glfw.SetCursorPositionCallback(WindowHandle, HandleMouseMoveEvent);
        
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
    }
    
    private void WindowToWorldPoint(Window window, double windowX, double windowY, out int worldX, out int worldY)
    {
        var renderer = _renderer;
        Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
        var wFactor = (float)renderer.Width / windowWidth;
        var hFactor = (float)renderer.Height / windowHeight;
        worldX = (int)(windowX * wFactor);
        worldY = (int)((windowHeight - windowY) * hFactor);
    }

    private void HandleFrameBufferSizeEvent(Window window, int width, int height)
    {
        glViewport(0, 0, width, height);
        _renderer.Render();
        Glfw.SwapBuffers(window);
    }

    private void HandleMouseButtonEvent(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
    {
        if (button != MouseButton.Left)
            return;

        if (state != InputState.Press)
            return;

        Glfw.GetCursorPosition(window, out var windowX, out var windowY);
        WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
        _renderer.AddItemAt(worldX, worldY);
    }

    private void HandleMouseMoveEvent(Window window, double windowX, double windowY)
    {
        WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
        _renderer.SetMousePosition(worldX, worldY);
    }
    
    protected override void OnUpdate()
    {
        _renderer.Render();
    }

    protected override void DisposeManagedResources()
    {
        _renderer.Dispose();
    }

    protected override void DisposeUnmanagedResources()
    {
        
    }
}