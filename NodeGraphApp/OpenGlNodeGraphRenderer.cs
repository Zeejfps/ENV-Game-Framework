using System.Numerics;
using MsdfBmpFont;
using NodeGraphApp;
using OpenGL.NET;
using PngSharp.Api;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using TextAlignment = NodeGraphApp.TextAlignment;

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
    private ShaderProgramInfo _panelShader;
    private int _rectUniformLoc;
    private int _viewProjUniformLoc;
    private int _colorUniformLoc;
    private int _borderRadiusUniformLoc;
    private int _borderSizeUniformLoc;
    private int _borderColorUniformLoc;

    // Glyph data
    private ShaderProgramInfo _glyphShader;
    private int _glyphViewProjUniformLoc;
    private int _glyphRectUniformLoc;
    private int _glyphSpriteRectUniformLoc;
    private int _glyphTextureUniformLoc;
    private uint _fontTextureId;
    private readonly MsdfBmpFontMetrics _fontMetrics;

    // Curve data
    private uint _curveVao;
    private uint _curveVbo;
    private ShaderProgramInfo _curveShader;
    private int _curveP0UniformLoc;
    private int _curveP1UniformLoc;
    private int _curveP2UniformLoc;
    private int _curveP3UniformLoc;
    private int _curveThicknessUniformLoc;
    private int _curveProjectionUniformLoc;
    private int _curveColorUniformLoc;

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
        LoadCurveData();
        
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glClearColor(0.1176f, 0.1176f, 	0.1804f, 1f);
    }

    private unsafe void LoadCurveData()
    {
        uint vbo;
        glGenBuffers(1, &vbo);
        _curveVbo = vbo;

        uint vao;
        glGenVertexArrays(1, &vao);
        _curveVao = vao;

        glBindVertexArray(vao);

        var stepCount = 32;
        var steps = stackalloc float[stepCount];
        for (var i = 0; i < stepCount-1; i++)
            steps[i] = (float)i / stepCount;
        steps[stepCount-1] = 1f;

        glBindBuffer(GL_ARRAY_BUFFER, _curveVbo);
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(float) * stepCount), steps, GL_STATIC_DRAW);

        glEnableVertexAttribArray(0);
        glVertexAttribPointer(0, 1, GL_FLOAT, false, sizeof(float), (void*)0);

        _curveShader = NewShader()
            .WithVertexShader(App.ResolvePath("Assets/Shaders/curve_vert.glsl"))
            .WithFragmentShader(App.ResolvePath("Assets/Shaders/curve_frag.glsl"))
            .WithGeometryShader(App.ResolvePath("Assets/Shaders/curve_geom.glsl"))
            .Compile();

        _curveP0UniformLoc = GetUniformLocation(_curveShader.Id, "u_p0");
        _curveP1UniformLoc = GetUniformLocation(_curveShader.Id, "u_p1");
        _curveP2UniformLoc = GetUniformLocation(_curveShader.Id, "u_p2");
        _curveP3UniformLoc = GetUniformLocation(_curveShader.Id, "u_p3");
        _curveThicknessUniformLoc = GetUniformLocation(_curveShader.Id, "thickness");
        _curveProjectionUniformLoc = GetUniformLocation(_curveShader.Id, "projection");
        _curveColorUniformLoc = GetUniformLocation(_curveShader.Id, "u_Color");
    }

    private unsafe void LoadGlyphData()
    {
        _glyphShader = NewShader()
            .WithVertexShader(App.ResolvePath("Assets/Shaders/glyph_vert.glsl"))
            .WithFragmentShader(App.ResolvePath("Assets/Shaders/glyph_frag.glsl"))
            .Compile();
        
        _glyphRectUniformLoc = GetUniformLocation(_glyphShader.Id, "u_rect");
        _glyphViewProjUniformLoc = GetUniformLocation(_glyphShader.Id, "u_viewProjMat");
        _glyphTextureUniformLoc = GetUniformLocation(_glyphShader.Id, "tex");
        _glyphSpriteRectUniformLoc = GetUniformLocation(_glyphShader.Id, "u_spriteRect");
        
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
        var fullPath = App.ResolvePath($"Assets/Fonts/Inter/{pageName}");
        var decodedPng = Png.DecodeFromFile(fullPath);
        
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
        _panelShader = NewShader()
            .WithVertexShader(App.ResolvePath("Assets/Shaders/panel_vert.glsl"))
            .WithFragmentShader(App.ResolvePath("Assets/Shaders/panel_frag.glsl"))
            .Compile();

        _rectUniformLoc = GetUniformLocation(_panelShader.Id, "u_rect");
        _viewProjUniformLoc = GetUniformLocation(_panelShader.Id, "u_vp");
        _colorUniformLoc = GetUniformLocation(_panelShader.Id, "u_color");
        _borderRadiusUniformLoc = GetUniformLocation(_panelShader.Id, "u_borderRadius");
        _borderSizeUniformLoc = GetUniformLocation(_panelShader.Id, "u_borderSize");
        _borderColorUniformLoc = GetUniformLocation(_panelShader.Id, "u_borderColor");
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);

        var nodeGraph = _nodeGraph;

        var backgroundLinks = nodeGraph.BackgroundLinks.GetAll();
        foreach (var link in backgroundLinks)
            RenderLink(link);

        var nodes = nodeGraph.Nodes.GetAll();
        foreach (var node in nodes)
            RenderNode(node);
        
        var foregroundLinks = nodeGraph.ForegroundLinks.GetAll();
        foreach (var link in foregroundLinks)
            RenderLink(link);
        
        if (_nodeGraph.SelectionBox.IsVisible)
            RenderSelectionBox(_nodeGraph.SelectionBox);
    }

    private void RenderSelectionBox(SelectionBox selectionBox)
    {
        RenderVisualNode(new VisualNode
        {
            Bounds = selectionBox.Bounds,
            BorderSize = BorderSizeStyle.All(0.5f),
            BorderColor = Color.FromRGBA(0.2f, 0.6f, 1.0f, 1.0f),
            Color = Color.FromRGBA(0.2f, 0.6f, 1.0f, 0.2f),
        });
    }

    private void RenderLink(Link link)
    {
        var color = Color.FromRGBA(0.5333f, 0.5725f, 0.7490f, 1.0f);
        if (link.IsSelected)
            color = Color.FromRGBA(0.0f, 0.7490f, 1.0f, 1.0f);
        else if (link.IsHovered)
            color = Color.FromRGBA(0.8667f, 0.9059f, 0.9784f, 1.0f);
                
        RenderCurve(new CubicCurve
        {
            P0 = link.P0,
            P1 = link.P1,
            P2 = link.P2,
            P3 = link.P3,
            Color = color,
        });
    }

    public unsafe void Teardown()
    {
        var vao = _quadVao;
        glDeleteBuffers(1, &vao);
    }

    private void RenderNode(Node node)
    {
        RenderVisualNode(node);
    }

    private unsafe void RenderVisualNode(VisualNode r)
    {
        glUseProgram(_panelShader.Id);
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
        //Console.WriteLine($"Base: {baseOffset}, Line Height: {fontFile.Common.LineHeight}");

        // NOTE(Zee): This is the top of the line
        var baseLine = bounds.Top;
        var cursor = lineStart;
        
        var topPadding = 0f;
        if (verticalTextAlignment == TextAlignment.Center)
        {
            var offset = (fontFile.Common.LineHeight - fontFile.Common.Base) * 0.5f;
            topPadding = (bounds.Height - fontFile.Common.LineHeight * fontScale) * 0.5f - offset * fontScale;;
        }
        
        // RenderVisualNode(new VisualNode
        // {
        //     Bounds = ScreenRect.FromLBWH(bounds.Left, bounds.Bottom, bounds.Width, bounds.Height),
        //     Color = Color.FromRGBA(0f, 1f, 0f, 1f)
        // });
        
        // RenderVisualNode(new VisualNode
        // {
        //     Bounds = ScreenRect.FromLTWH(bounds.Left, bounds.Top - topPadding , bounds.Width, fontFile.Common.LineHeight * fontScale),
        //     Color = Color.FromRGBA(0.5f, 0.5f, 0f, 1f)
        // });
        //
        // RenderVisualNode(new VisualNode
        // {
        //     Bounds = ScreenRect.FromLTWH(bounds.Left, bounds.Top - topPadding - 4*fontScale , bounds.Width, fontFile.Common.Base * fontScale),
        //     Color = Color.FromRGBA(1f, 0.5f, 0.2f, 1f)
        // });

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
            var bottom = baseLine - topPadding - glyphInfo.YOffset*fontScale - glyphInfo.Height*fontScale;
            var width = glyphInfo.Width * fontScale;
            var height = glyphInfo.Height * fontScale;

            var uOffset = glyphInfo.X / scaleW;
            var vOffset = glyphInfo.Y / scaleH;
            var uScale = glyphInfo.Width / scaleW;
            var vScale = glyphInfo.Height / scaleH;
            
            // RenderVisualNode(new VisualNode
            // {
            //     Bounds = ScreenRect.FromLBWH(left, bottom , width, height),
            //     Color = Color.FromRGBA(1f, 0f, 1f, 1f)
            // });
            
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
        
        glUseProgram(_glyphShader.Id);
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

    private unsafe void RenderCurve(CubicCurve curve)
    {
        glUseProgram(_curveShader.Id);
        glBindVertexArray(_curveVao);
        glUniform2f(_curveP0UniformLoc, curve.P0.X, curve.P0.Y);
        glUniform2f(_curveP1UniformLoc, curve.P1.X, curve.P1.Y);
        glUniform2f(_curveP2UniformLoc, curve.P2.X, curve.P2.Y);
        glUniform2f(_curveP3UniformLoc, curve.P3.X, curve.P3.Y);
        glUniform4f(_curveColorUniformLoc, curve.Color.R, curve.Color.G, curve.Color.B, curve.Color.A);
        glUniform1f(_curveThicknessUniformLoc, 1f);
        var viewProjMat = _camera.ViewProjectionMatrix;
        var ptr = &viewProjMat.M11;
        glUniformMatrix4fv(_curveProjectionUniformLoc, 1, false, ptr);        
        glDrawArrays(GL_POINTS, 0, 1);
    }
}

public readonly struct CubicCurve
{
    public const int Steps = 32;
    public Vector2 P0 { get; init; }
    public Vector2 P1 { get; init; }
    public Vector2 P2 { get; init; }
    public Vector2 P3 { get; init; }
    public Color Color { get; init; }
}