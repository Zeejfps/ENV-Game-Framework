using GLFW;
using static GL46;
using Monitor = GLFW.Monitor;

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

while (!Glfw.WindowShouldClose(windowHandle))
{
    Glfw.PollEvents();
    
    glClear(GL_COLOR_BUFFER_BIT);
            
    Glfw.SwapBuffers(windowHandle);
}

Glfw.Terminate();