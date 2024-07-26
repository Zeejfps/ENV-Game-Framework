// See https://aka.ms/new-console-template for more information

using GLFW;
using OpenGlWrapper;
using Monitor = GLFW.Monitor;

Glfw.Init();

var window = Glfw.CreateWindow(640, 480, "Test", new Monitor(), Window.None);
Glfw.MakeContextCurrent(window);

var context = OpenGlContext.Init(Glfw.GetProcAddress);
var vaoManager = context.VertexArrayObjectManager;
var arrayBufferManager = context.ArrayBufferManager;

var vbo = arrayBufferManager.CreateAndBind();
Span<float> vertexData = stackalloc float[]
{
    -1f, -1f, 0f, // Position
    +0f, +0f,     // UVs
    
    -1f, +1f, 0f, // Position
    +0f, +1f,     // UVs
    
    +1f, -1f, 0f, // Position
    +1f, +0f,     // UVs
};
arrayBufferManager.AllocFixedSizedAndUploadData<float>(vertexData, FixedSizedBufferAccessFlag.None);

Console.WriteLine($@"IsAllocated: {arrayBufferManager.IsAllocated(vbo)}");
Console.WriteLine($@"IsFixedSize: {arrayBufferManager.IsFixedSize(vbo)}");

var vao = vaoManager.CreateAndBind();
vaoManager
    .EnableAndBindAttrib(0, vbo, 3, GlType.Float, false, 3 + 2, 0)
    .EnableAndBindAttrib(1, vbo, 2, GlType.Float, false, 2 + 3, 3);

arrayBufferManager.Destroy(vbo);
vaoManager.Destroy(vao);

Glfw.SetWindowShouldClose(window, true);
Glfw.Terminate();