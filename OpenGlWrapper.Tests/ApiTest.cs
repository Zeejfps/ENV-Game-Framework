using System.Numerics;
using GLFW;
using OpenGlWrapper;
using OpenGlWrapper.Buffers;
using Monitor = GLFW.Monitor;

public sealed class ApiTest
{
    public void Launch()
    {
        Glfw.Init();

        var window = Glfw.CreateWindow(640, 480, "Test", new Monitor(), Window.None);
        Glfw.MakeContextCurrent(window);

        var context = OpenGlContext.Init(Glfw.GetProcAddress);
        var vaoManager = context.VertexArrayObjectManager;
        var vboManager = context.ArrayBufferManager;
        var framebufferManager = context.FramebufferManager;
        var shaderProgramManager = context.ShaderProgramManager;
        var textureManager = context.Texture2dManager;

        Glfw.SetFramebufferSizeCallback(window,
            (_, width, height) => { framebufferManager.SetViewport(0, 0, width, height); });

        // Not needed, Window Framebuffer is the default bound buffer
        framebufferManager.Bind(FramebufferId.WindowFramebuffer);

        var vbo = vboManager.CreateAndBind();
        var vao = vaoManager.CreateAndBind();
        var textureHandle = textureManager.CreateAndBind();

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

        // vboManager.AllocFixedSizedAndUploadData<Vertex>(vertices, FixedSizedBufferAccessFlag.None);
        
        vboManager.AllocFixedSize<Vertex>(vertices.Length, FixedSizedBufferAccessFlag.ReadWrite);
        using (var memory = vboManager.MapReadWrite<Vertex>())
        {
            for (var i = 0; i < vertices.Length; i++)
                memory.Write(i, vertices[i]);
        }

        using (var memory = vboManager.MapRead<float>())
        {
            Console.WriteLine($"Float count: {memory.Count}");
            var v1 = memory.Read(0);
            var v2 = memory.Read(1);
            var v3 = memory.Read(2);
            var v4 = memory.Read(2);

            Console.WriteLine($"V1: {v1}");
            Console.WriteLine($"V2: {v2}");
            Console.WriteLine($"V3: {v3}");
            Console.WriteLine($"V4: {v4}");
        }


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

        textureManager.Destroy(textureHandle);
        shaderProgramManager.Destroy(shaderProgram);
        vboManager.Destroy(vbo);
        vaoManager.Destroy(vao);

        Glfw.Terminate();

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
    }
}

public struct Vertex
{
    public Vector2 Position;
    public Vector2 UVs;

    public override string ToString()
    {
        return $"{nameof(Position)}: {Position}, {nameof(UVs)}: {UVs}";
    }
}