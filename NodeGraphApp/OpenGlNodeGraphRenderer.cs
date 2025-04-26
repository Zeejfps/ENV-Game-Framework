using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

public sealed class OpenGlNodeGraphRenderer
{
    private readonly float[] _vertices =
    [
        // X, Y
        0.0f,  0.5f,   // Top
        -0.5f, -0.5f,   // Bottom Left
        0.5f, -0.5f    // Bottom Right
    ];

    private readonly NodeGraph _nodeGraph;

    private uint _quadVao;
    private uint _quadVbo;
    private uint _shader;

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph)
    {
        _nodeGraph = nodeGraph;
    }

    public unsafe void Setup()
    {
        Span<uint> vbos = stackalloc uint[1];
        fixed (uint* ptr = &vbos[0])
        {
            glGenBuffers(1, ptr);
            AssertNoGlError();
            _quadVbo = vbos[0];
        }

        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        _quadVao = vao;

        _shader = new ShaderProgramBuilder()
            .WithVertexShader("Assets/simple_vert.glsl")
            .WithFragmentShader("Assets/simple_frag.glsl")
            .Build();

        Console.WriteLine(_shader);

        glBindBuffer(GL_ARRAY_BUFFER, _quadVbo);
        var size = new IntPtr(sizeof(float) * _vertices.Length);
        fixed (void* dataPtr = &_vertices[0])
            glBufferData(GL_ARRAY_BUFFER, size, dataPtr, GL_STATIC_DRAW);

        glBindVertexArray(_quadVao);
        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 3, GL_FLOAT, false, sizeof(float) * 3, (void*)0);

        glClearColor(0f, 0f, 0f, 1f);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);

        var nodeGraph = _nodeGraph;
        var nodes = nodeGraph.Nodes.GetAll();
        foreach (var node in nodes)
        {
            RenderNode(node);
        }
    }

    public unsafe void Teardown()
    {
        var buffers = stackalloc uint[1];
        buffers[0] = _quadVao;
        glDeleteBuffers(1, buffers);
    }

    private void RenderNode(Node node)
    {
        glUseProgram(_shader);
        glBindVertexArray(_quadVao);
        glDrawArrays(GL_TRIANGLES, 0, 6);
    }
}