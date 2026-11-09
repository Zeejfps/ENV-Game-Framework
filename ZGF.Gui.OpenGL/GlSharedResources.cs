using System.Runtime.InteropServices;
using ZGF.Fonts;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.OpenGL;

public sealed unsafe class GlSharedResources : IDisposable
{
    private uint _rectShader, _glyphShader, _imageShader, _shadowShader, _shapeShader;
    private uint _unitQuadVbo;
    private uint _fontAtlasTextureId;
    private int _uploadedAtlasVersion = -1;
    private readonly FreeTypeFontBackend _fonts;
    private readonly GlImageManager _imageManager;
    private bool _isDisposed;

    public uint RectShader => _rectShader;
    public uint GlyphShader => _glyphShader;
    public uint ImageShader => _imageShader;
    public uint ShadowShader => _shadowShader;
    public uint ShapeShader => _shapeShader;
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
            .Compile();
        _glyphShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_glyph.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_glyph.frag.glsl"))
            .Compile();
        _imageShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_image.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_image.frag.glsl"))
            .Compile();
        _shadowShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_shadow.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_shadow.frag.glsl"))
            .Compile();
        _shapeShader = new ShaderProgramCompiler()
            .WithVertexShaderSource(ShaderAssets.LoadShaderSource("canvas_shape.vert.glsl"))
            .WithFragmentShaderSource(ShaderAssets.LoadShaderSource("canvas_shape.frag.glsl"))
            .Compile();

        SetupUnitQuad();
        SetupFontAtlas();
    }

    public void UploadAtlasIfDirty(ref int uploadCount)
    {
        // A grown atlas is a new pixel buffer of a new size: reallocate the texture's storage and
        // upload all of it, rather than poking a sub-rect into storage that is now the wrong shape.
        if (_fonts.AtlasVersion != _uploadedAtlasVersion)
        {
            UploadWholeAtlas();
            uploadCount++;
            return;
        }

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
        uint tex;
        glGenTextures(1, &tex);
        glBindTexture(GL_TEXTURE_2D, tex);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        AssertNoGlError();

        _fontAtlasTextureId = tex;
        UploadWholeAtlas();
    }

    private void UploadWholeAtlas()
    {
        var pixels = _fonts.AtlasPixels;

        glBindTexture(GL_TEXTURE_2D, _fontAtlasTextureId);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
        fixed (byte* ptr = &MemoryMarshal.GetReference(pixels))
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_R8, _fonts.AtlasWidth, _fonts.AtlasHeight, 0,
                GL_RED, GL_UNSIGNED_BYTE, ptr);
        AssertNoGlError();

        _uploadedAtlasVersion = _fonts.AtlasVersion;
        _fonts.ClearDirty();
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
