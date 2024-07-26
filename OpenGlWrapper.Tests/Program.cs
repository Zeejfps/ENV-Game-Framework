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
Span<float> vertexData = stackalloc float[]
{
    0f, 0f, 0f,
    1f, 1f, 1f,
    0f, 0f,
    1f, 1f
};
arrayBufferManager.AllocFixedSizedAndUploadData<float>(vertexData, FixedSizedBufferAccessFlag.None);

arrayBufferManager.Destroy(vao);

Glfw.SetWindowShouldClose(window, true);
Glfw.Terminate();