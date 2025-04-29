using System.Numerics;
using GLFW;
using MsdfBmpFont;
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
var n1 = new Node
{
    Title = "Node 1",
    Bounds = new ScreenRect
    {
        Left = 0,
        Bottom = 10,
        Width = 40,
        Height = 20
    },
    InputPorts =
    {
        new InputPort(),
        new InputPort(),
        new InputPort(),
        new InputPort(),
        new InputPort(),
    },
    OutputPorts =
    {
        new OutputPort(),
        new OutputPort(),
    }
};
n1.Update();

var n2 = new Node
{
    Title = "Node 31",
    Bounds = new ScreenRect
    {
        Left = -50,
        Bottom = 10,
        Width = 40,
        Height = 20
    },
    InputPorts =
    {
        new InputPort(),
        new InputPort(),
        new InputPort(),
    },
    OutputPorts =
    {
        new OutputPort(),
    }
};
n2.Update();

nodeGraph.Nodes.Add(n1);
nodeGraph.Nodes.Add(n2);

var mouse = new Mouse();
var keyboard = new Keyboard();
var camera = new Camera(windowAspectRatio);
var viewport = new Viewport(window, camera)
{
    Bounds = ScreenRect.FromLeftTopWidthHeight(0, 1f, 1f, 1f)
};
var fontLoader = new MsdfBmpFontFileLoader();
var interFontData = fontLoader.LoadFromFilePath("Assets/Fonts/Inter/Inter_28pt-Regular-msdf.json");
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera, interFontData);
var cameraDragController = new CameraDragController(viewport, mouse, keyboard);
var nodeSelectionController = new NodeSelectionController(viewport, mouse, nodeGraph);

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
    viewport.Update();
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

GL46.glEnable(GL46.GL_SCISSOR_TEST);
renderer.Setup();
while (!Glfw.WindowShouldClose(window))
{
    mouse.Update();
    keyboard.Update();
    Glfw.PollEvents();
    cameraDragController.Update();
    nodeSelectionController.Update();
    viewport.Update();
    renderer.Update();
    Glfw.SwapBuffers(window);
}
renderer.Teardown();

Glfw.Terminate();