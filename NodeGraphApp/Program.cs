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
};
n1.AddInputPort();
n1.AddInputPort();
n1.AddInputPort();
n1.AddInputPort();
n1.AddInputPort();
n1.AddOutputPort();
n1.AddOutputPort();
var testOutputPort = n1.AddOutputPort();

var n2 = new Node
{
    Title = "Node 31",
    Bounds = new ScreenRect
    {
        Left = -50,
        Bottom = 10,
        Width = 40,
        Height = 20
    }
};
n2.AddInputPort();
n2.AddInputPort();
n2.AddInputPort();
n2.AddOutputPort();

nodeGraph.Nodes.Add(n1);
nodeGraph.Nodes.Add(n2);

// var link = new Link
// {
//     StartPosition = Vector2.Zero,
//     EndPosition = new Vector2(50, 50)
// };
// nodeGraph.Links.Add(link);
//
// nodeGraph.Links.Connect(link, testOutputPort);

var mouse = new Mouse();
var keyboard = new Keyboard();
var camera = new Camera(windowAspectRatio);
var viewport = new Viewport(window, camera)
{
    Bounds = ScreenRect.FromLeftTopWidthHeight(0, 1f, 1f, 1f)
};
var mousePicker = new MousePicker(viewport, mouse, nodeGraph);
var fontLoader = new MsdfBmpFontFileLoader();
var interFontData = fontLoader.LoadFromFilePath(App.ResolvePath("Assets/Fonts/Inter/Inter_28pt-Regular-msdf.json"));
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera, interFontData);
var cameraDragController = new CameraDragController(viewport, mouse, keyboard);
var keyboardAndMouseController = new KeyboardAndMouseController(mousePicker, nodeGraph, keyboard);

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
    
    n1.Update();
    n2.Update();
    
    cameraDragController.Update();
    mousePicker.Update();
    keyboardAndMouseController.Update();

    nodeGraph.Update();
    viewport.Update();
    renderer.Update();
    
    Glfw.SwapBuffers(window);
}
renderer.Teardown();

Glfw.Terminate();