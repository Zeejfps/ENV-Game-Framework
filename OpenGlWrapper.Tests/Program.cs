// See https://aka.ms/new-console-template for more information

using GLFW;
using OpenGlWrapper;
using Monitor = GLFW.Monitor;

Console.WriteLine("Hello, World!");

Glfw.Init();

var window = Glfw.CreateWindow(640, 480, "Test", new Monitor(), Window.None);
Glfw.MakeContextCurrent(window);

var context = new OpenGlContext(Glfw.GetProcAddress);
var arrayBufferManager = context.ArrayBufferManager;

var vao = arrayBufferManager.CreateAndBind();
arrayBufferManager.AllocImmutableAndUpload();

arrayBufferManager.Destroy(vao);

Glfw.SetWindowShouldClose(window, true);
Glfw.Terminate();