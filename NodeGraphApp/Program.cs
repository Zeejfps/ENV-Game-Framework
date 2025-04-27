using System.Numerics;
using GLFW;
using NodeGraphApp;
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
    Bounds = new ScreenRect
    {
        Left = 0,
        Bottom = 10,
        Width = 40,
        Height = 20
    },
});
nodeGraph.Nodes.Add(new Node
{
    Bounds = new ScreenRect
    {
        Left = -50,
        Bottom = 10,
        Width = 40,
        Height = 20
    },
});

var mouse = new Mouse();
var keyboard = new Keyboard();
var camera = new Camera(windowAspectRatio);
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera);
var cameraDragController = new CameraDragController(window, camera, mouse, keyboard);
var nodeSelectionController = new NodeSelectionController(window, mouse, camera, nodeGraph);

Glfw.SetMouseButtonCallback(window, (_, button, state, _) =>
{
    if (state == InputState.Press)
    {
        mouse.PressButton(button);
    }
    else if (state == InputState.Release)
    {
        mouse.ReleaseButton(button);
    }
});

Glfw.SetCursorPositionCallback(window, (_, x, y) =>
{
    mouse.Position = new Vector2((float)x, (float)y);
});

Glfw.SetKeyCallback(window, (_, key, code, state, mods) =>
{
    if (state == InputState.Press)
    {
        keyboard.PressKey(key);
    }
    else if (state == InputState.Release)
    {
        keyboard.ReleaseKey(key);
    }
});

Glfw.SetWindowSizeCallback(window, (window, width, height) =>
{
    GL46.glViewport(0, 0, width, height);
    var aspectRatio = (float)width / height;
    camera.AspectRatio = aspectRatio;
    renderer.Update();
    Glfw.SwapBuffers(window);
});

Glfw.SetScrollCallback(window, (window, dx, dy) =>
{
    camera.ZoomFactor += (float)dy * 0.05f;
});

Glfw.MakeContextCurrent(window);
Glfw.ShowWindow(window);
Glfw.SwapInterval(1);

GL46.Import(Glfw.GetProcAddress);

renderer.Setup();
while (!Glfw.WindowShouldClose(window))
{
    mouse.Update();
    keyboard.Update();
    Glfw.PollEvents();
    cameraDragController.Update();
    nodeSelectionController.Update();
    renderer.Update();
    Glfw.SwapBuffers(window);
}
renderer.Teardown();

Glfw.Terminate();