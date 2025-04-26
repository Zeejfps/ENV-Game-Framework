using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

public sealed class OpenGlNodeGraphRenderer
{
    private readonly float[] _vertices =
    [
        // X, Y
        1f,  1f,   // Top Right
        1f, -0f,   // Bottom Right
        -0f,  1f,   // Top Left

        -0f,  1f,   // Top Left
        1f, -0f,   // Bottom Right
        -0f, -0f    // Bottom Left
    ];

    private readonly NodeGraph _nodeGraph;
    private readonly Camera _camera;

    private uint _quadVao;
    private uint _quadVbo;
    private uint _shader;
    private int _rectUniformLoc;
    private int _viewProjUniformLoc;

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph, Camera camera)
    {
        _nodeGraph = nodeGraph;
        _camera = camera;
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

        _rectUniformLoc = GetUniformLocation(_shader, "u_rect");
        _viewProjUniformLoc = GetUniformLocation(_shader, "u_vp");

        glBindBuffer(GL_ARRAY_BUFFER, _quadVbo);
        var size = new IntPtr(sizeof(float) * _vertices.Length);
        fixed (void* dataPtr = &_vertices[0])
            glBufferData(GL_ARRAY_BUFFER, size, dataPtr, GL_STATIC_DRAW);

        glBindVertexArray(_quadVao);
        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 3, GL_FLOAT, false, sizeof(float) * 2, (void*)0);

        glClearColor(0f, 0f, 0f, 1f);
    }

    public void Update()
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
        var vao = _quadVao;
        glDeleteBuffers(1, &vao);
    }

    private unsafe void RenderNode(Node node)
    {
        glUseProgram(_shader);
        glUniform4f(_rectUniformLoc, node.XPos, node.YPos, node.Width, node.Height);
        var viewProjMat = _camera.ViewProjectionMatrix;
        var ptr = &viewProjMat.M11;
        glUniformMatrix4fv(_viewProjUniformLoc, 1, false, ptr);
        glBindVertexArray(_quadVao);
        glDrawArrays(GL_TRIANGLES, 0,  6);
    }
}