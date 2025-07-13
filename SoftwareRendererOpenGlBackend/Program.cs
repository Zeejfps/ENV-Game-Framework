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
var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, "Software Renderer", Monitor.None, Window.None);

Glfw.MakeContextCurrent(windowHandle);
Glfw.ShowWindow(windowHandle);
Glfw.SwapInterval(1);

Import(Glfw.GetProcAddress);
AssertNoGlError();

var renderer = new Renderer();

void HandleFrameBufferSizeEvent(Window window, int width, int height)
{
    glViewport(0, 0, width, height);
    renderer.Render();
    Glfw.SwapBuffers(window);
}

void HandleMouseButtonEvent(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
{
    if (button != MouseButton.Left)
        return;

    if (state != InputState.Press)
        return;

    Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);
    Glfw.GetCursorPosition(window, out var windowX, out var windowY);

    var wFactor = (float)renderer.Width / windowWidth;
    var hFactor = (float)renderer.Height / windowHeight;
    var worldX = (int)(windowX * wFactor);
    var worldY = (int)((windowHeight - windowY) * hFactor);

    renderer.AddItemAt(worldX, worldY);
}

Glfw.SetFramebufferSizeCallback(windowHandle, HandleFrameBufferSizeEvent);
Glfw.SetMouseButtonCallback(windowHandle, HandleMouseButtonEvent);

glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

while (!Glfw.WindowShouldClose(windowHandle))
{
    Glfw.PollEvents();
    renderer.Render();
    Glfw.SwapBuffers(windowHandle);
}

Glfw.Terminate();