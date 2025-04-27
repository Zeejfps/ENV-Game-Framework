using System.Numerics;
using MsdfBmpFont;
using NodeGraphApp;
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
    private readonly MsdfBmpFontLoader _fontLoader;
    private readonly FontData _interFontData;

    private uint _quadVao;
    private uint _quadVbo;
    private uint _shader;
    private int _rectUniformLoc;
    private int _viewProjUniformLoc;
    private int _colorUniformLoc;
    private int _borderRadiusUniformLoc;
    private int _borderSizeUniformLoc;
    private int _borderColorUniformLoc;

    private readonly Dictionary<int, Glyph> _glyphsByCodePoint = new();

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph, Camera camera, FontData interFontData)
    {
        _nodeGraph = nodeGraph;
        _camera = camera;
        _interFontData = interFontData;
        foreach (var glyph in _interFontData.Glyphs)
            _glyphsByCodePoint[glyph.Id] = glyph;
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
        RenderVisualNode(node.VisualNode);
    }

    private unsafe void RenderVisualNode(VisualNode r)
    {
        glUseProgram(_shader);
        glUniform4f(_rectUniformLoc, r.Bounds.Left, r.Bounds.Bottom, r.Bounds.Width, r.Bounds.Height);
        glUniform4f(_colorUniformLoc, r.Color.R, r.Color.G, r.Color.B, r.Color.A);
        glUniform4f(_borderColorUniformLoc, r.BorderColor.R, r.BorderColor.G, r.BorderColor.B, r.BorderColor.A);
        glUniform4f(_borderRadiusUniformLoc, r.BorderRadius.TopLeft, r.BorderRadius.TopRight, r.BorderRadius.BottomRight, r.BorderRadius.BottomLeft);
        glUniform4f(_borderSizeUniformLoc, r.BorderSize.Top, r.BorderSize.Right, r.BorderSize.Bottom, r.BorderSize.Left);
        var viewProjMat = _camera.ViewProjectionMatrix;
        var ptr = &viewProjMat.M11;
        glUniformMatrix4fv(_viewProjUniformLoc, 1, false, ptr);
        glBindVertexArray(_quadVao);
        glDrawArrays(GL_TRIANGLES, 0,  6);

        if (r.Text != null)
        {
            RenderText(r.Bounds, r.Text);
        }

        foreach (var child in r.Children)
            RenderVisualNode(child);
    }

    private void RenderText(ScreenRect bounds, string text)
    {
        var left = bounds.Left;
        var cursor = new Vector2(left, bounds.Bottom);
        var fontFile = _interFontData;
        var baseOffset = fontFile.Common.Base;
        var scaleW = (float)fontFile.Common.ScaleW;
        var scaleH = (float)fontFile.Common.ScaleH;
        var lineHeight = (float)fontFile.Common.LineHeight;

        foreach (var codePoint in AsCodePoints(text))
        {
            if (codePoint == '\n')
            {
                cursor.X = left;
                cursor.Y -= lineHeight;
                continue;
            }

            if (!_glyphsByCodePoint.TryGetValue(codePoint, out var glyphInfo))
                continue;

            var xPos = cursor.X + glyphInfo.XOffset;

            var fontScale = 1f;
            var offsetFromTop = glyphInfo.YOffset - (baseOffset - glyphInfo.Height);
            var yPos = cursor.Y - offsetFromTop * fontScale;
            var width = glyphInfo.Width * fontScale;
            var height = glyphInfo.Height * fontScale;

            var uOffset = glyphInfo.X / scaleW;
            var vOffset = glyphInfo.Y / scaleH;
            var uScale = glyphInfo.Width / scaleW;
            var vScale = glyphInfo.Height / scaleH;

            cursor.X += glyphInfo.XAdvance * fontScale;
        }
    }

    private IEnumerable<int> AsCodePoints(string s)
    {
        for(int i = 0; i < s.Length; ++i)
        {
            yield return char.ConvertToUtf32(s, i);
            if(char.IsHighSurrogate(s, i))
                i++;
        }
    }
}