using static GL46;
using GLFW;
using ZGF.Geometry;
using ZGF.GlfwUtils;
using ZnvQuadTree;

namespace SoftwareRendererOpenGlBackend;

public sealed class QuadTreeRendererApp : OpenGlApp
{
    private readonly QuadTreeRenderer _renderer;
    private readonly SizeCallback _framebufferSizeCallback;
    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _cursorPositonCallback;
    
    public QuadTreeRendererApp(StartupConfig startupConfig) : base(startupConfig)
    {
        var framebufferWidth = startupConfig.WindowWidth / 2;
        var framebufferHeight = startupConfig.WindowHeight / 2;
        _renderer = new QuadTreeRenderer(
            framebufferWidth,  
            framebufferHeight,
            new QuadTreePointF<Item>(new RectF
            {
                Bottom = 0,
                Left = 0,
                Width = framebufferWidth,
                Height = framebufferHeight
            }, 6, maxDepth: 5)
        );

        _framebufferSizeCallback = HandleFrameBufferSizeEvent;
        _mouseButtonCallback = HandleMouseButtonEvent;
        _cursorPositonCallback = HandleMouseMoveEvent;
        
        Glfw.SetFramebufferSizeCallback(WindowHandle, _framebufferSizeCallback);
        Glfw.SetMouseButtonCallback(WindowHandle, _mouseButtonCallback);
        Glfw.SetCursorPositionCallback(WindowHandle, _cursorPositonCallback);
        
        glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
    }
    
    private void WindowToWorldPoint(Window window, double windowX, double windowY, out int worldX, out int worldY)
    {
        var renderer = _renderer;
        Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
        var wFactor = (float)renderer.FramebufferWidth / windowWidth;
        var hFactor = (float)renderer.FramebufferHeight / windowHeight;
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