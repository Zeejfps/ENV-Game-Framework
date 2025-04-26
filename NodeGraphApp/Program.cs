using System.Numerics;
using GLFW;
using Monitor = GLFW.Monitor;

Glfw.Init();


Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
Glfw.WindowHint(Hint.ContextVersionMajor, 4);
Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

var windowWidth = 1280;
var windowHeight = 720;
var windowAspectRatio = (float)windowWidth / windowHeight;
var window = Glfw.CreateWindow(windowWidth, windowHeight, "Node Graph", Monitor.None, Window.None);

var nodeGraph = new NodeGraph();
nodeGraph.Nodes.Add(new Node
{
    XPos = 10f,
    YPos = 10f,
    Width = 40,
    Height = 20
});
nodeGraph.Nodes.Add(new Node
{
    XPos = -50f,
    YPos = 10f,
    Width = 40,
    Height = 20
});
var camera = new Camera(windowAspectRatio);
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera);

Glfw.SetWindowSizeCallback(window, (window, width, height) =>
{
    var aspectRatio = (float)width / height;
    camera.AspectRatio = aspectRatio;
    renderer.Render();
    Glfw.SwapBuffers(window);
});

Glfw.SetScrollCallback(window, (window, dx, dy) =>
{
    camera.ZoomFactor += (float)dy * 0.05f;
});

Glfw.SetMouseButtonCallback(window, (w, button, state, modifiers) =>
{
    if (modifiers == ModifierKeys.Alt && button == MouseButton.Left && state == InputState.Press)
    {
        //TODO: Start drag
    }
});

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