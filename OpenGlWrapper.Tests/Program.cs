// See https://aka.ms/new-console-template for more information

using System.Numerics;
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

Glfw.SetFramebufferSizeCallback(window, (_, width, height) =>
{
    framebufferManager.SetViewport(0, 0, width, height);
});

// Not needed, Window Framebuffer is the default bound buffer
framebufferManager.Bind(FramebufferId.WindowFramebuffer);

var vbo = vboManager.CreateAndBind();
var vao = vaoManager.CreateAndBind();

Span<Vertex> vertices = stackalloc Vertex[]
{
    new Vertex
    {
        Position = new Vector2(-1f, 1f),
        UVs = new Vector2(0, 1f)
    },
    new Vertex
    {
        Position = new Vector2(-1f, -1f),
        UVs = new Vector2(0, 0)
    },
    new Vertex
    {
        Position = new Vector2(1f, -1f),
        UVs = new Vector2(1f, 0f)
    },
    
    new Vertex
    {
        Position = new Vector2(-1f, 1f),
        UVs = new Vector2(0, 1f)
    },
    new Vertex
    {
        Position = new Vector2(1f, -1f),
        UVs = new Vector2(1, 0)
    },
    new Vertex
    {
        Position = new Vector2(1f, 1f),
        UVs = new Vector2(1f, 1f)
    },
};
var vertexTemplate = vaoManager.CreateTemplate<Vertex>();

vboManager.AllocFixedSizedAndUploadData<Vertex>(vertices, FixedSizedBufferAccessFlag.None);

vaoManager.EnableAndBindAttrib(vertexTemplate, 0, vbo);
vaoManager.EnableAndBindAttrib(vertexTemplate, 1, vbo);

var shaderProgram = shaderProgramManager.CompileFromSourceFiles(
    "Assets/simple.vert.glsl",
    "Assets/simple.frag.glsl"
);

var vertexCount = vertices.Length;
framebufferManager.SetClearColor(0.3f, 0.1f, 0.5f, 1f);
while (!Glfw.WindowShouldClose(window))
{
    framebufferManager.Clear(ClearFlags.ColorBuffer);
    framebufferManager.DrawArrayOfTriangles(shaderProgram, vao, vertexCount);

    Glfw.PollEvents();
    Glfw.SwapBuffers(window);
}

shaderProgramManager.Destroy(shaderProgram);
vboManager.Destroy(vbo);
vaoManager.Destroy(vao);

Glfw.Terminate();

public struct Vertex
{
    public Vector2 Position;
    public Vector2 UVs;
}


// Span<float> vertexData = stackalloc float[]
// {
//     -1f, -1f, +1f, // Position
//     +0f, +0f,     // UVs
//     
//     -1f, +1f, +1f, // Position
//     +0f, +1f,     // UVs
//     
//     +1f, -1f, +1f, // Position
//     +1f, +0f,     // UVs
// };
// vboManager.AllocFixedSizedAndUploadData<float>(vertexData, FixedSizedBufferAccessFlag.None);
//
// Console.WriteLine($@"IsAllocated: {vboManager.IsAllocated(vbo)}");
// Console.WriteLine($@"IsFixedSize: {vboManager.IsFixedSize(vbo)}");
//
// vaoManager
//     .EnableAndBindAttrib(0, vbo, 3, GlType.Float, false, 3 + 2, 0)
//     .EnableAndBindAttrib(1, vbo, 2, GlType.Float, false, 2 + 3, 3);
