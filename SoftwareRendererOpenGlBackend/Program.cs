using GLFW;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using Monitor = GLFW.Monitor;

unsafe
{
    Glfw.Init();

    Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
    Glfw.WindowHint(Hint.ContextVersionMajor, 4);
    Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

    var windowWidth = 1280;
    var windowHeight = 720;
    var windowAspectRatio = (float)windowWidth / windowHeight;
    var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, "Node Graph", Monitor.None, Window.None);

    Glfw.MakeContextCurrent(windowHandle);
    Glfw.ShowWindow(windowHandle);
    Glfw.SwapInterval(1);

    Import(Glfw.GetProcAddress);
    AssertNoGlError();

    uint textureId;
    glGenTextures(1, &textureId);
    AssertNoGlError();
    
    glBindTexture(GL_TEXTURE_2D, textureId);
    AssertNoGlError();
    
    glActiveTexture(GL_TEXTURE0);
    AssertNoGlError();
    
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
    AssertNoGlError();


    while (!Glfw.WindowShouldClose(windowHandle))
    {
        Glfw.PollEvents();
    
        glClear(GL_COLOR_BUFFER_BIT);
            
        Glfw.SwapBuffers(windowHandle);
    }

    glDeleteTextures(1, &textureId);
    Glfw.Terminate();
}