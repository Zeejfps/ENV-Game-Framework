// See https://aka.ms/new-console-template for more information

using GLFW;
using OpenGlWrapper;
using Monitor = GLFW.Monitor;

Glfw.Init();

var window = Glfw.CreateWindow(640, 480, "Test", new Monitor(), Window.None);
Glfw.MakeContextCurrent(window);

var context = OpenGlContext.Init(Glfw.GetProcAddress);
var vaoManager = context.VertexArrayObjectManager;
var vboManager = context.ArrayBufferManager;
var framebufferManager = context.FramebufferManager;
var shaderProgramManager = context.ShaderProgramManager;

// Not needed, Window Framebuffer is the default bound buffer
framebufferManager.Bind(FramebufferId.WindowFramebuffer);

var vbo = vboManager.CreateAndBind();
Span<float> vertexData = stackalloc float[]
{
    -1f, -1f, +1f, // Position
    +0f, +0f,     // UVs
    
    -1f, +1f, +1f, // Position
    +0f, +1f,     // UVs
    
    +1f, -1f, +1f, // Position
    +1f, +0f,     // UVs
};
vboManager.AllocFixedSizedAndUploadData<float>(vertexData, FixedSizedBufferAccessFlag.None);

Console.WriteLine($@"IsAllocated: {vboManager.IsAllocated(vbo)}");
Console.WriteLine($@"IsFixedSize: {vboManager.IsFixedSize(vbo)}");

var vao = vaoManager.CreateAndBind();
vaoManager
    .EnableAndBindAttrib(0, vbo, 3, GlType.Float, false, 3 + 2, 0)
    .EnableAndBindAttrib(1, vbo, 2, GlType.Float, false, 2 + 3, 3);

var shaderProgram = shaderProgramManager.CompileFromSourceFiles(
    "Assets/simple.vert.glsl",
    "Assets/simple.frag.glsl"
);

framebufferManager.SetClearColor(0.3f, 0.1f, 0.5f, 1f);
while (!Glfw.WindowShouldClose(window))
{
    framebufferManager.Clear(ClearFlags.ColorBuffer);
    framebufferManager.DrawArrayOfTriangles(shaderProgram, vao, 3);
    
    Glfw.PollEvents();
    Glfw.SwapBuffers(window);
}

vboManager.Destroy(vbo);
vaoManager.Destroy(vao);

Glfw.Terminate();