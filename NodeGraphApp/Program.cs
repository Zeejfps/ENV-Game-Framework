using GLFW;
using Monitor = GLFW.Monitor;

Glfw.Init();

var window = Glfw.CreateWindow(1280, 720, "Node Graph", Monitor.None, Window.None);

Glfw.ShowWindow(window);

Glfw.Terminate();