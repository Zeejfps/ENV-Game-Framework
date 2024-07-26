// See https://aka.ms/new-console-template for more information

using GLFW;
using OpenGlWrapper;
using Monitor = GLFW.Monitor;

Glfw.Init();

var window = Glfw.CreateWindow(640, 480, "Test", new Monitor(), Window.None);
Glfw.MakeContextCurrent(window);

var context = OpenGlContext.Init(Glfw.GetProcAddress);
var arrayBufferManager = context.ArrayBufferManager;

var vbo = arrayBufferManager.CreateAndBind();
Span<float> vertexData = stackalloc float[]
{
    0f, 0f, 0f,
    1f, 1f, 1f,
    0f, 0f,
    1f, 1f
};
arrayBufferManager.AllocFixedSizedAndUploadData<float>(vertexData, FixedSizedBufferAccessFlag.None);

Console.WriteLine($"IsAllocated: {arrayBufferManager.IsAllocated(vbo)}");
Console.WriteLine($"IsFixedSize: {arrayBufferManager.IsFixedSize(vbo)}");

arrayBufferManager.Destroy(vbo);

Glfw.SetWindowShouldClose(window, true);
Glfw.Terminate();