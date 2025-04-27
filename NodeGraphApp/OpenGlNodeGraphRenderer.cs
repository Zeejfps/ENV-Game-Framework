using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

public sealed class OpenGlNodeGraphRenderer
{
    private readonly float[] _vertices =
    [
        // X, Y
        1f,  1f,   // Right Top
        1f,  0f,   // Right Bottom
        0f,  1f,   // Left Top

        0f,  1f,   // Left Top
        1f,  0f,   // Right Bottom
        0f,  0f    // Left Bottom
    ];

    private readonly NodeGraph _nodeGraph;
    private readonly Camera _camera;

    private uint _quadVao;
    private uint _quadVbo;
    private uint _shader;
    private int _rectUniformLoc;
    private int _viewProjUniformLoc;
    private int _colorUniformLoc;
    private int _borderRadiusUniformLoc;
    private int _borderSizeUniformLoc;
    private int _borderColorUniformLoc;

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph, Camera camera)
    {
        _nodeGraph = nodeGraph;
        _camera = camera;
    }

    public unsafe void Setup()
    {
        uint vbo;
        glGenBuffers(1, &vbo);
        AssertNoGlError();
        _quadVbo = vbo;

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
        _colorUniformLoc = GetUniformLocation(_shader, "u_color");
        _borderRadiusUniformLoc = GetUniformLocation(_shader, "u_borderRadius");
        _borderSizeUniformLoc = GetUniformLocation(_shader, "u_borderSize");
        _borderColorUniformLoc = GetUniformLocation(_shader, "u_borderColor");

        glBindBuffer(GL_ARRAY_BUFFER, _quadVbo);
        var size = new IntPtr(sizeof(float) * _vertices.Length);
        fixed (void* dataPtr = &_vertices[0])
            glBufferData(GL_ARRAY_BUFFER, size, dataPtr, GL_STATIC_DRAW);

        glBindVertexArray(_quadVao);
        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 3, GL_FLOAT, false, sizeof(float) * 2, (void*)0);

        glClearColor(0.1176f, 0.1176f, 	0.1804f, 1f);
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

    private void RenderNode(Node node)
    {
        var borderColor = node.IsHovered
            ? Color.FromRGBA(0.2f, 0.6f, 0.7333f, 1.0f)
            : Color.FromRGBA(0f, 0f, 0f, 1f);

        RenderRectangle(new Rectangle
        {
            Left = node.XPos,
            Bottom = node.YPos,
            Width = node.Width,
            Height = node.Height,
            Color = Color.FromRGBA(0.1765f, 0.1922f, 0.2588f, 1f),
            BorderSize = BorderSizeStyle.All(0.25f),
            BorderRadius = BorderRadiusStyle.All(0.25f),
            BorderColor = borderColor
        });

        RenderRectangle(new Rectangle
        {
            Left = node.XPos+0.25f,
            Bottom = node.YPos + 14.75f,
            Width = node.Width - 0.5f,
            Height = 5f,
            Color = Color.FromRGBA(0.2314f, 0.2588f, 0.3412f, 1.0f),
            BorderSize = BorderSizeStyle.FromLTRB(0f, 0f, 0f, 0.25f)
        });

        RenderRectangle(new Rectangle
        {
            Left = node.XPos+1f,
            Bottom = node.YPos + 10f,
            Width = 2f,
            Height = 2f,
            Color = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f),
            BorderColor = Color.FromRGBA(0.1f, 0.2588f, 0.7412f, 1.0f),
            BorderSize = BorderSizeStyle.All(0.25f),
            BorderRadius = BorderRadiusStyle.All(1f)
        });

        RenderRectangle(new Rectangle
        {
            Left = node.XPos+1f,
            Bottom = node.YPos + 6f,
            Width = 2f,
            Height = 2f,
            Color = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f),
            BorderColor = Color.FromRGBA(0.2f, 0.6588f, 0.3412f, 1.0f),
            BorderSize = BorderSizeStyle.All(0.25f),
            BorderRadius = BorderRadiusStyle.All(1f)
        });

        RenderRectangle(new Rectangle
        {
            Left = node.XPos+1f,
            Bottom = node.YPos + 2f,
            Width = 2f,
            Height = 2f,
            Color = Color.FromRGBA(0.1f, 0.1f, 0.1f, 1.0f),
            BorderColor = Color.FromRGBA(0.5f, 0.2588f, 0.3412f, 1.0f),
            BorderSize = BorderSizeStyle.All(0.25f),
            BorderRadius = BorderRadiusStyle.All(1f)
        });
    }

    private unsafe void RenderRectangle(Rectangle r)
    {
        glUseProgram(_shader);
        glUniform4f(_rectUniformLoc, r.Left, r.Bottom, r.Width, r.Height);
        glUniform4f(_colorUniformLoc, r.Color.R, r.Color.G, r.Color.B, r.Color.A);
        glUniform4f(_borderColorUniformLoc, r.BorderColor.R, r.BorderColor.G, r.BorderColor.B, r.BorderColor.A);
        glUniform4f(_borderRadiusUniformLoc, r.BorderRadius.Top, r.BorderRadius.Right, r.BorderRadius.Bottom, r.BorderRadius.Left);
        glUniform4f(_borderSizeUniformLoc, r.BorderSize.Top, r.BorderSize.Right, r.BorderSize.Bottom, r.BorderSize.Left);
        var viewProjMat = _camera.ViewProjectionMatrix;
        var ptr = &viewProjMat.M11;
        glUniformMatrix4fv(_viewProjUniformLoc, 1, false, ptr);
        glBindVertexArray(_quadVao);
        glDrawArrays(GL_TRIANGLES, 0,  6);
    }
}

public readonly struct Rectangle
{
    public float Left { get; init; }
    public float Bottom { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public Color Color { get; init; }
    public Color BorderColor { get; init; }
    public BorderSizeStyle BorderSize { get; init; }
    public BorderRadiusStyle BorderRadius { get; init; }
}