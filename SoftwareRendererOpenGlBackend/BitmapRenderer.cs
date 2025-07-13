using OpenGL.NET;
using SoftwareRendererModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;
using static OpenGL.NET.GLTexture;

namespace SoftwareRendererOpenGlBackend;

public sealed unsafe class BitmapRenderer : IDisposable
{
    private readonly Bitmap _bitmap;
    private readonly ShaderProgramInfo _shaderProgram;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ibo;
    private readonly Texture _texture;

    private bool _isDisposed;

    public BitmapRenderer(Bitmap bitmap)
    {
        _bitmap = bitmap;
        var texture = new Texture2DBuilder()
            .WithMinFilter(TextureMinFilter.Nearest)
            .WithMagFilter(TextureMagFilter.Nearest)
            .BindAndBuild();
        AssertNoGlError();

        glTexImage2D<uint>(texture, 0,  GL_RGBA8, bitmap.Width, bitmap.Height,
            GL_RGBA, GL_UNSIGNED_BYTE, bitmap.Pixels);
        AssertNoGlError();

        _shaderProgram = new ShaderProgramCompiler()
            .WithVertexShader("Assets/tex.vert.glsl")
            .WithFragmentShader("Assets/tex.frag.glsl")
            .Compile();

        float[] vertices =
        [
            // Positions        // Texture Coords
            1.0f,  1.0f, 0.0f,  1.0f, 1.0f, // top right
            1.0f, -1.0f, 0.0f,  1.0f, 0.0f, // bottom right
            -1.0f, -1.0f, 0.0f,  0.0f, 0.0f, // bottom left
            -1.0f,  1.0f, 0.0f,  0.0f, 1.0f  // top left
        ];

        uint[] indices =
        [
            0, 1, 3, // first triangle
            1, 2, 3  // second triangle
        ];

        uint vbo, vao, ibo;
        glGenVertexArrays(1, &vao);
        glGenBuffers(1, &vbo);
        glGenBuffers(1, &ibo);

        glBindVertexArray(vao);

        var vertexDataBuffer = glBindBuffer<float>(GL_ARRAY_BUFFER, vbo);
        glBufferData(vertexDataBuffer, vertices, BufferUsageHint.StaticDraw);
        AssertNoGlError();

        glVertexAttribPointer<float>(
            attribIndex: 0,
            count: 3,
            stride: 5,
            offset: 0
        );
        AssertNoGlError();

        glVertexAttribPointer<float>(
            attribIndex: 1,
            count: 2,
            stride: 5,
            offset: 3
        );
        AssertNoGlError();

        glEnableVertexAttribArray(0);
        glEnableVertexAttribArray(1);

        var indexDataBuffer = glBindBuffer<uint>(GL_ELEMENT_ARRAY_BUFFER, ibo);
        glBufferData(indexDataBuffer, indices, BufferUsageHint.StaticDraw);

        _vao = vao;
        _vbo = vbo;
        _ibo = ibo;
        _texture = texture;
    }

    public void Render()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(BitmapRenderer));

        glClear(GL_COLOR_BUFFER_BIT);

        fixed (void* pixelDataPtr = &_bitmap.Pixels[0])
        {
            glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0,
                _bitmap.Width, _bitmap.Height, GL_RGBA, GL_UNSIGNED_BYTE, pixelDataPtr);
        }

        glUseProgram(_shaderProgram.Id);
        glBindVertexArray(_vao);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, (void*)0);
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        var vao = _vao;
        var vbo = _vbo;
        var ibo = _ibo;
        var shaderProgram = _shaderProgram;

        glDeleteVertexArrays(1, &vao);
        glDeleteBuffers(1, &vbo);
        glDeleteBuffers(1, &ibo);
        glDeleteProgram(shaderProgram.Id);

        var textureId = _texture.Id;
        glDeleteTextures(1, &textureId);
    }
}