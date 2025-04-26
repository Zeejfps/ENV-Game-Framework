using GLFW;
using Monitor = GLFW.Monitor;

Glfw.Init();


Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
Glfw.WindowHint(Hint.ContextVersionMajor, 4);
Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
var window = Glfw.CreateWindow(1280, 720, "Node Graph", Monitor.None, Window.None);

var nodeGraph = new NodeGraph();
nodeGraph.Nodes.Add(new Node
{
    XPos = 0,
    YPos = 0,
    Width = 100,
    Height = 50
});
var camera = new Camera();
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera);

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