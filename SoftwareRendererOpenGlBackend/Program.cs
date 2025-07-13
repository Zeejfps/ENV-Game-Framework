using GLFW;
using OpenGL.NET;
using SoftwareRendererModule;
using SoftwareRendererOpenGlBackend;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;
using static OpenGL.NET.GLTexture;
using Monitor = GLFW.Monitor;

unsafe
{
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

    void FrameBufferSizeCallback(Window window, int width, int height)
    {
        glViewport(0, 0, width, height);
        renderer.Render();
        Glfw.SwapBuffers(window);
    }

    Glfw.SetFramebufferSizeCallback(windowHandle, FrameBufferSizeCallback);
    glClearColor(0.2f, 0.3f, 0.3f, 1.0f);

    while (!Glfw.WindowShouldClose(windowHandle))
    {
        Glfw.PollEvents();
        renderer.Render();
        Glfw.SwapBuffers(windowHandle);
    }

    Glfw.Terminate();
}