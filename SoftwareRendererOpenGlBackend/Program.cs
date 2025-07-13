using GLFW;
using SoftwareRendererOpenGlBackend;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

Glfw.Init();

Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
Glfw.WindowHint(Hint.ContextVersionMajor, 4);
Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

var windowWidth = 640;
var windowHeight = 480;
var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, "Quad Tree Renderer", Monitor.None, Window.None);

Glfw.MakeContextCurrent(windowHandle);
Glfw.ShowWindow(windowHandle);
Glfw.SwapInterval(1);

Import(Glfw.GetProcAddress);
AssertNoGlError();

var renderer = new QuadTreeRenderer();

void HandleFrameBufferSizeEvent(Window window, int width, int height)
{
    glViewport(0, 0, width, height);
    renderer.Render();
    Glfw.SwapBuffers(window);
}

void WindowToWorldPoint(Window window, double windowX, double windowY, out int worldX, out int worldY)
{
    Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
    var wFactor = (float)renderer.Width / windowWidth;
    var hFactor = (float)renderer.Height / windowHeight;
    worldX = (int)(windowX * wFactor);
    worldY = (int)((windowHeight - windowY) * hFactor);
}

void HandleMouseButtonEvent(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
{
    if (button != MouseButton.Left)
        return;

    if (state != InputState.Press)
        return;

    Glfw.GetCursorPosition(window, out var windowX, out var windowY);
    WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
    renderer.AddItemAt(worldX, worldY);
}

void HandleMouseMoveEvent(Window window, double windowX, double windowY)
{
    WindowToWorldPoint(window, windowX, windowY, out var worldX, out var worldY);
    renderer.SetMousePosition(worldX, worldY);
}

Glfw.SetFramebufferSizeCallback(windowHandle, HandleFrameBufferSizeEvent);
Glfw.SetMouseButtonCallback(windowHandle, HandleMouseButtonEvent);
Glfw.SetCursorPositionCallback(windowHandle, HandleMouseMoveEvent);

glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

while (!Glfw.WindowShouldClose(windowHandle))
{
    Glfw.PollEvents();
    renderer.Render();
    Glfw.SwapBuffers(windowHandle);
}

Glfw.Terminate();