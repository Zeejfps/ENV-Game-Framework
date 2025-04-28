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

    // Shared data
    private uint _quadVao;
    private uint _quadVbo;

    // Panel data
    private uint _panelShader;
    private int _rectUniformLoc;
    private int _viewProjUniformLoc;
    private int _colorUniformLoc;
    private int _borderRadiusUniformLoc;
    private int _borderSizeUniformLoc;
    private int _borderColorUniformLoc;

    // Glyph data
    private uint _glyphShader;
    private int _glyphViewProjUniformLoc;
    private int _glyphRectUniformLoc;

    private readonly Dictionary<int, Glyph> _glyphsByCodePoint = new();

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph, Camera camera, FontData interFontData)
    {
        _nodeGraph = nodeGraph;
        _camera = camera;
        _interFontData = interFontData;
        foreach (var glyph in _interFontData.Glyphs)
            _glyphsByCodePoint[glyph.Id] = glyph;
    }

    public void Setup()
    {
        LoadSharedData();
        LoadPanelData();
        LoadGlyphData();
        
        glClearColor(0.1176f, 0.1176f, 	0.1804f, 1f);
    }

    private void LoadGlyphData()
    {
        _glyphShader = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/Glyph/glyph_vert.glsl")
            .WithFragmentShader("Assets/Shaders/Glyph/glyph_frag.glsl")
            .Build();
        
        _glyphRectUniformLoc = GetUniformLocation(_glyphShader, "u_rect");
        _glyphViewProjUniformLoc = GetUniformLocation(_glyphShader, "u_viewProjMat");
    }

    private unsafe void LoadSharedData()
    {
        uint vbo;
        glGenBuffers(1, &vbo);
        AssertNoGlError();
        _quadVbo = vbo;

        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        _quadVao = vao;
        
        glBindBuffer(GL_ARRAY_BUFFER, _quadVbo);
        var size = new IntPtr(sizeof(float) * _vertices.Length);
        fixed (void* dataPtr = &_vertices[0])
            glBufferData(GL_ARRAY_BUFFER, size, dataPtr, GL_STATIC_DRAW);

        glBindVertexArray(_quadVao);
        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 3, GL_FLOAT, false, sizeof(float) * 2, (void*)0);
    }

    private void LoadPanelData()
    {
        _panelShader = new ShaderProgramBuilder()
            .WithVertexShader("Assets/simple_vert.glsl")
            .WithFragmentShader("Assets/simple_frag.glsl")
            .Build();

        _rectUniformLoc = GetUniformLocation(_panelShader, "u_rect");
        _viewProjUniformLoc = GetUniformLocation(_panelShader, "u_vp");
        _colorUniformLoc = GetUniformLocation(_panelShader, "u_color");
        _borderRadiusUniformLoc = GetUniformLocation(_panelShader, "u_borderRadius");
        _borderSizeUniformLoc = GetUniformLocation(_panelShader, "u_borderSize");
        _borderColorUniformLoc = GetUniformLocation(_panelShader, "u_borderColor");
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
        glUseProgram(_panelShader);
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
        var lineStart = bounds.Left;
        var cursor = new Vector2(lineStart, bounds.Bottom);
        var fontFile = _interFontData;
        var baseOffset = fontFile.Common.Base;
        var scaleW = (float)fontFile.Common.ScaleW;
        var scaleH = (float)fontFile.Common.ScaleH;
        var lineHeight = (float)fontFile.Common.LineHeight;

        foreach (var codePoint in AsCodePoints(text))
        {
            if (codePoint == '\n')
            {
                cursor.X = lineStart;
                cursor.Y -= lineHeight;
                continue;
            }

            if (!_glyphsByCodePoint.TryGetValue(codePoint, out var glyphInfo))
                continue;
            
            var left = cursor.X + glyphInfo.XOffset;

            var fontScale = 0.1f;
            var offsetFromTop = glyphInfo.YOffset - (baseOffset - glyphInfo.Height);
            var bottom = cursor.Y - offsetFromTop * fontScale;
            var width = glyphInfo.Width * fontScale;
            var height = glyphInfo.Height * fontScale;

            var uOffset = glyphInfo.X / scaleW;
            var vOffset = glyphInfo.Y / scaleH;
            var uScale = glyphInfo.Width / scaleW;
            var vScale = glyphInfo.Height / scaleH;
            
            RenderGlyph(new GlyphRect
            {
                Bounds = ScreenRect.FromLBWH(left, bottom, width, height),
                SpriteRect = ScreenRect.FromLBWH(uOffset, vOffset, uScale, vScale)
            });

            cursor.X += glyphInfo.XAdvance * fontScale;
        }
    }

    private unsafe void RenderGlyph(GlyphRect glyph)
    {
        var bounds = glyph.Bounds;
        glUseProgram(_glyphShader);
        glUniform4f(_glyphRectUniformLoc, bounds.Left, bounds.Bottom, bounds.Width, bounds.Height);
        var viewProjMat = _camera.ViewProjectionMatrix;
        var ptr = &viewProjMat.M11;
        glUniformMatrix4fv(_glyphViewProjUniformLoc, 1, false, ptr);
        glBindVertexArray(_quadVao);
        glDrawArrays(GL_TRIANGLES, 0,  6);
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

public readonly struct GlyphRect
{
    public ScreenRect Bounds { get; init; }
    public ScreenRect SpriteRect { get; init; }
}