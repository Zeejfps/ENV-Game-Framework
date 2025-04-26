using GLFW;
using Monitor = GLFW.Monitor;

Glfw.Init();


Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
Glfw.WindowHint(Hint.ContextVersionMajor, 4);
Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
var window = Glfw.CreateWindow(1280, 720, "Node Graph", Monitor.None, Window.None);

var nodeGraph = new NodeGraph();
var renderer = new OpenGlNodeGraphRenderer(nodeGraph);

Glfw.MakeContextCurrent(window);
Glfw.ShowWindow(window);

GL46.Import(Glfw.GetProcAddress);

renderer.Setup();
while (!Glfw.WindowShouldClose(window))
{
    Glfw.PollEvents();
    renderer.Render();
    Glfw.SwapBuffers(window);
}
renderer.Teardown();

Glfw.Terminate();