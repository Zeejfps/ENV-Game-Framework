using System.Numerics;
using EasyGameFramework.GUI;
using MsdfBmpFont;
using NodeGraphApp;
using OpenGLSandbox;
using PngSharp.Api;
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
    private readonly MsdfBmpFontFileLoader _fontFileLoader;
    private readonly MsdfFontFile _interFontData;

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
    private int _glyphSpriteRectUniformLoc;
    private int _glyphTextureUniformLoc;
    private uint _fontTextureId;

    private readonly MsdfBmpFontMetrics _fontMetrics;

    public OpenGlNodeGraphRenderer(NodeGraph nodeGraph, Camera camera, MsdfFontFile interFontData)
    {
        _nodeGraph = nodeGraph;
        _camera = camera;
        _interFontData = interFontData;
        _fontMetrics = new MsdfBmpFontMetrics(interFontData);
    }

    public void Setup()
    {
        LoadSharedData();
        LoadPanelData();
        LoadGlyphData();
        
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glClearColor(0.1176f, 0.1176f, 	0.1804f, 1f);
    }

    private unsafe void LoadGlyphData()
    {
        _glyphShader = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/Glyph/glyph_vert.glsl")
            .WithFragmentShader("Assets/Shaders/Glyph/glyph_frag.glsl")
            .Build();
        
        _glyphRectUniformLoc = GetUniformLocation(_glyphShader, "u_rect");
        _glyphViewProjUniformLoc = GetUniformLocation(_glyphShader, "u_viewProjMat");
        _glyphTextureUniformLoc = GetUniformLocation(_glyphShader, "tex");
        _glyphSpriteRectUniformLoc = GetUniformLocation(_glyphShader, "u_spriteRect");
        
        uint textureId;
        glGenTextures(1, &textureId); AssertNoGlError();
        _fontTextureId = textureId;
        
        glActiveTexture(GL_TEXTURE0); AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, textureId); AssertNoGlError();

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);

        var pageName = _interFontData.Pages[0];
        var api = new PngApi();
        var decodedPng = api.DecodeFromFile($"Assets/Fonts/Inter/{pageName}");
        
        var width = decodedPng.Width;
        var height = decodedPng.Height;

        fixed(void* ptr = &decodedPng.PixelData[0]) 
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr); AssertNoGlError();
    }

    private unsafe void LoadSharedData()
    {
        uint vbo;
        glGenBuffers(1, &vbo); AssertNoGlError();
        _quadVbo = vbo;

        uint vao;
        glGenVertexArrays(1, &vao); AssertNoGlError();
        _quadVao = vao;

        glBindBuffer(GL_ARRAY_BUFFER, _quadVbo); AssertNoGlError();
        var size = new IntPtr(sizeof(float) * _vertices.Length);
        fixed (void* dataPtr = &_vertices[0])
            glBufferData(GL_ARRAY_BUFFER, size, dataPtr, GL_STATIC_DRAW); AssertNoGlError();

        glBindVertexArray(_quadVao); AssertNoGlError();
        glEnableVertexAttribArray(0); AssertNoGlError();
        glVertexAttribPointer(0, 3, GL_FLOAT, false, sizeof(float) * 2, (void*)0); AssertNoGlError();
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
            RenderText(r.Bounds, r.TextVerticalAlignment, r.Text);

        foreach (var child in r.Children)
            RenderVisualNode(child);
    }
    
    private void RenderText(ScreenRect bounds, TextAlignment verticalTextAlignment, string text)
    {
        var fontScale = 0.1f;

        var lineStart = bounds.Left;
        var fontFile = _interFontData;
        var baseOffset = fontFile.Common.Base;
        var scaleW = (float)fontFile.Common.ScaleW;
        var scaleH = (float)fontFile.Common.ScaleH;
        var lineHeight = fontFile.Common.LineHeight * fontScale;
        Console.WriteLine($"Base: {baseOffset}, Line Height: {fontFile.Common.LineHeight}");

        // NOTE(Zee): This is the top of the line
        var baseLine = bounds.Top;
        var cursor = lineStart;
        
        var topPadding = 0f;
        if (verticalTextAlignment == TextAlignment.Center)
        {
            topPadding = (lineHeight / 2.0f - baseOffset);
        }

        int? prevCodePoint = null;
        foreach (var codePoint in AsCodePoints(text))
        {
            if (codePoint == '\n')
            {
                cursor = lineStart;
                baseLine -= lineHeight;
                continue;
            }

            if (!_fontMetrics.TryGetGlyphInfo(codePoint, out var glyphInfo))
                continue;
            
            var kerningOffset = 0;
            if (prevCodePoint.HasValue &&
                _fontMetrics.TryGetKerningInfo(prevCodePoint.Value, codePoint, out var kerningInfo))
            {
                kerningOffset = kerningInfo.Amount;
            }
            
            var left = cursor + (glyphInfo.XOffset + kerningOffset) * fontScale;
            var bottom = baseLine - (glyphInfo.YOffset + glyphInfo.Height) * fontScale;
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

            cursor += glyphInfo.XAdvance * fontScale;
            
            prevCodePoint = codePoint;
        }
    }

    private unsafe void RenderGlyph(GlyphRect glyph)
    {
        var bounds = glyph.Bounds;
        var spriteRect = glyph.SpriteRect;
        
        glUseProgram(_glyphShader);
        glUniform1i(_glyphTextureUniformLoc, 0);
        glUniform4f(_glyphRectUniformLoc, bounds.Left, bounds.Bottom, bounds.Width, bounds.Height);
        glUniform4f(_glyphSpriteRectUniformLoc, spriteRect.Left, spriteRect.Bottom, spriteRect.Width, spriteRect.Height);
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