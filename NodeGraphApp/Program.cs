using System.Numerics;
using GLFW;
using MsdfBmpFont;
using NodeGraphApp;
using Monitor = GLFW.Monitor;
using Window = NodeGraphApp.Window;

Glfw.Init();


Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
Glfw.WindowHint(Hint.ContextVersionMajor, 4);
Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

var windowWidth = 1280;
var windowHeight = 720;
var windowAspectRatio = (float)windowWidth / windowHeight;
var windowHandle = Glfw.CreateWindow(windowWidth, windowHeight, "Node Graph", Monitor.None, GLFW.Window.None);

var nodeGraph = new NodeGraph();
var n1 = new Node
{
    Title = "Node 1",
    Bounds = new RectF
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
n1.AddOutputPort();

var n2 = new Node
{
    Title = "Node 31",
    Bounds = new RectF
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

var mouse = new Mouse();
var keyboard = new Keyboard();
var window = new Window();
var camera = new Camera(windowAspectRatio);
var viewport = new Viewport(window, camera)
{
    Bounds = RectF.FromLeftTopWidthHeight(0, 1f, 1f, 1f)
};
var mousePicker = new MousePicker(viewport, mouse, nodeGraph);
var fontLoader = new MsdfBmpFontFileLoader();
var interFontData = fontLoader.LoadFromFilePath(App.ResolvePath("Assets/Fonts/Inter/Inter_28pt-Regular-msdf.json"));
var renderer = new OpenGlNodeGraphRenderer(nodeGraph, camera, interFontData);
var cameraDragController = new CameraDragController(viewport, mouse, keyboard);
var nodeGraphController = new NodeGraphKeyboardAndMouseController(nodeGraph, camera, mousePicker, keyboard);
var glfwMouseController = new GlfwMouseController(windowHandle, mouse);
var glfwKeyboardController = new GlfwKeyboardController(windowHandle, keyboard);
var glfwWindowController = new GlfwWindowController(windowHandle, window, viewport, renderer);

Glfw.MakeContextCurrent(windowHandle);
Glfw.ShowWindow(windowHandle);
Glfw.SwapInterval(1);

GL46.Import(Glfw.GetProcAddress);

GL46.glEnable(GL46.GL_SCISSOR_TEST);
renderer.Setup();
while (!Glfw.WindowShouldClose(windowHandle))
{
    glfwMouseController.Update();
    glfwKeyboardController.Update();
    glfwWindowController.Update();
    Glfw.PollEvents();

    n1.Update();
    n2.Update();
    
    cameraDragController.Update();
    mousePicker.Update();
    nodeGraphController.Update();

    nodeGraph.Update();
    viewport.Update();
    renderer.Update();
    
    Glfw.SwapBuffers(windowHandle);
}
renderer.Teardown();

Glfw.Terminate();