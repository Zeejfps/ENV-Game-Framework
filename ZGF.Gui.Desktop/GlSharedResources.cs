using System.Runtime.InteropServices;
using OpenGL.NET;
using ZGF.Fonts;
using ZGF.Gui.Desktop.Backends.OpenGl;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Desktop;

public sealed unsafe class GlSharedResources : IDisposable
{
    private uint _rectShader, _glyphShader, _imageShader, _shadowShader, _shapeShader;
    private int _rectProjLoc, _glyphProjLoc, _imageProjLoc, _shadowProjLoc, _shapeProjLoc;
    private int _glyphAtlasLoc, _imageTexLoc;
    private uint _unitQuadVbo;
    private uint _fontAtlasTextureId;
    private readonly FreeTypeFontBackend _fonts;
    private readonly GlImageManager _imageManager;
    private bool _isDisposed;

    public uint RectShader => _rectShader;
    public uint GlyphShader => _glyphShader;
    public uint ImageShader => _imageShader;
    public uint ShadowShader => _shadowShader;
    public uint ShapeShader => _shapeShader;
    public int RectProjLoc => _rectProjLoc;
    public int GlyphProjLoc => _glyphProjLoc;
    public int ImageProjLoc => _imageProjLoc;
    public int ShadowProjLoc => _shadowProjLoc;
    public int ShapeProjLoc => _shapeProjLoc;
    public int GlyphAtlasLoc => _glyphAtlasLoc;
    public int ImageTexLoc => _imageTexLoc;
    public uint UnitQuadVbo => _unitQuadVbo;
    public uint FontAtlasTextureId => _fontAtlasTextureId;
    public GlImageManager ImageManager => _imageManager;
    public FreeTypeFontBackend Fonts => _fonts;

    public GlSharedResources(FreeTypeFontBackend fonts, GlImageManager imageManager)
    {
        _fonts = fonts;
        _imageManager = imageManager;

        _rectShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_rect.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_rect.frag.glsl"))
            .Compile().Id;
        _glyphShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_glyph.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_glyph.frag.glsl"))
            .Compile().Id;
        _imageShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_image.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_image.frag.glsl"))
            .Compile().Id;
        _shadowShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_shadow.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_shadow.frag.glsl"))
            .Compile().Id;
        _shapeShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_shape.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_shape.frag.glsl"))
            .Compile().Id;

        _rectProjLoc = glGetUniformLocation(_rectShader, "u_projection");
        _glyphProjLoc = glGetUniformLocation(_glyphShader, "u_projection");
        _imageProjLoc = glGetUniformLocation(_imageShader, "u_projection");
        _shadowProjLoc = glGetUniformLocation(_shadowShader, "u_projection");
        _shapeProjLoc = glGetUniformLocation(_shapeShader, "u_projection");
        _glyphAtlasLoc = glGetUniformLocation(_glyphShader, "u_atlas");
        _imageTexLoc = glGetUniformLocation(_imageShader, "u_texture");

        BindClipBlockToZero(_rectShader);
        BindClipBlockToZero(_glyphShader);
        BindClipBlockToZero(_imageShader);
        BindClipBlockToZero(_shadowShader);
        BindClipBlockToZero(_shapeShader);

        glUseProgram(_glyphShader);
        glUniform1i(_glyphAtlasLoc, 0);
        glUseProgram(_imageShader);
        glUniform1i(_imageTexLoc, 0);

        SetupUnitQuad();
        SetupFontAtlas();
    }

    public void UploadAtlasIfDirty(ref int uploadCount)
    {
        if (!_fonts.AtlasDirty) return;

        var rect = _fonts.DirtyRect;
        if (rect.IsEmpty)
        {
            _fonts.ClearDirty();
            return;
        }

        var pixels = _fonts.AtlasPixels;
        glBindTexture(GL_TEXTURE_2D, _fontAtlasTextureId);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        glPixelStorei(GL_UNPACK_ROW_LENGTH, _fonts.AtlasWidth);

        var offset = rect.Y * _fonts.AtlasWidth + rect.X;
        fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            glTexSubImage2D(GL_TEXTURE_2D, 0, rect.X, rect.Y, rect.Width, rect.Height,
                GL_RED, GL_UNSIGNED_BYTE, ptr + offset);

        glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);
        AssertNoGlError();

        _fonts.ClearDirty();
        uploadCount++;
    }

    private void SetupUnitQuad()
    {
        Span<float> verts = stackalloc float[12]
        {
            0f, 0f, 1f, 0f, 0f, 1f,
            1f, 0f, 1f, 1f, 0f, 1f,
        };
        uint vbo;
        glGenBuffers(1, &vbo);
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        fixed (float* ptr = &verts[0])
            glBufferData(GL_ARRAY_BUFFER, verts.Length * sizeof(float), ptr, GL_STATIC_DRAW);
        AssertNoGlError();
        _unitQuadVbo = vbo;
    }

    private void SetupFontAtlas()
    {
        var width = _fonts.AtlasWidth;
        var height = _fonts.AtlasHeight;
        var pixels = _fonts.AtlasPixels;

        uint tex;
        glGenTextures(1, &tex);
        glBindTexture(GL_TEXTURE_2D, tex);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_R8, width, height, 0, GL_RED, GL_UNSIGNED_BYTE, ptr);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        AssertNoGlError();

        _fontAtlasTextureId = tex;
        _fonts.ClearDirty();
    }

    private static void BindClipBlockToZero(uint shader)
    {
        Span<byte> name = stackalloc byte[10];
        name[0] = (byte)'C'; name[1] = (byte)'l'; name[2] = (byte)'i'; name[3] = (byte)'p';
        name[4] = (byte)'R'; name[5] = (byte)'e'; name[6] = (byte)'c'; name[7] = (byte)'t';
        name[8] = (byte)'s'; name[9] = 0;
        uint blockIndex;
        fixed (byte* p = name)
            blockIndex = glGetUniformBlockIndex(shader, p);
        if (blockIndex != GL_INVALID_INDEX)
            glUniformBlockBinding(shader, blockIndex, 0);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        if (_unitQuadVbo != 0) { var v = _unitQuadVbo; glDeleteBuffers(1, &v); _unitQuadVbo = 0; }
        if (_fontAtlasTextureId != 0) { var t = _fontAtlasTextureId; glDeleteTextures(1, &t); _fontAtlasTextureId = 0; }
        if (_rectShader != 0) { glDeleteProgram(_rectShader); _rectShader = 0; }
        if (_glyphShader != 0) { glDeleteProgram(_glyphShader); _glyphShader = 0; }
        if (_imageShader != 0) { glDeleteProgram(_imageShader); _imageShader = 0; }
        if (_shadowShader != 0) { glDeleteProgram(_shadowShader); _shadowShader = 0; }
        if (_shapeShader != 0) { glDeleteProgram(_shapeShader); _shapeShader = 0; }
    }
}
