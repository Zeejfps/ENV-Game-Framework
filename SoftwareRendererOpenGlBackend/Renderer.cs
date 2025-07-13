using OpenGL.NET;
using SoftwareRendererModule;
using ZnvQuadTree;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLBuffer;
using static OpenGL.NET.GLTexture;

namespace SoftwareRendererOpenGlBackend;

sealed class Item
{
    public PointF Position { get; set; }
}

public sealed unsafe class Renderer : IDisposable
{
    private readonly Bitmap _colorBuffer;
    private readonly ShaderProgramInfo _shaderProgram;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ibo;
    private readonly Texture _texture;
    private readonly QuadTreePointF<Item> _quadTree;

    private bool _isDisposed;

    public Renderer()
    {
        var colorBuffer = new Bitmap(320, 240);
        _quadTree = new QuadTreePointF<Item>(new RectF
        {
            Bottom = 0,
            Left = 0,
            Width = 320,
            Height = 240
        }, 5);

        var texture = new Texture2DBuilder()
            .WithMinFilter(TextureMinFilter.Nearest)
            .WithMagFilter(TextureMagFilter.Nearest)
            .BindAndBuild();
        AssertNoGlError();

        glTexImage2D<uint>(texture, 0,  GL_RGBA8, colorBuffer.Width, colorBuffer.Height,
            GL_RGBA, GL_UNSIGNED_BYTE, colorBuffer.Pixels);
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
        _colorBuffer = colorBuffer;
    }

    public void Render()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(Renderer));

        var colorBuffer = _colorBuffer;

        glClear(GL_COLOR_BUFFER_BIT);
        colorBuffer.Fill(0x000000);

        var quadTreeInfo = _quadTree.GetInfo();
        foreach (var nodeInfo in quadTreeInfo.Nodes)
        {
            var bounds = nodeInfo.Bounds;
            Graphics.DrawRect(colorBuffer,
                (int)bounds.Left, (int)bounds.Bottom,
                (int)bounds.Width, (int)bounds.Height, 0x00FF00);
        }

        // Graphics.DrawRect(colorBuffer, 300, 200, 100, 150, 0xFF00FF);
        // Graphics.FillRect(colorBuffer, 0, 0, 100, 150, 0xFF00FF);
        // Graphics.DrawLine(colorBuffer, 0, 200, 100, 200, 0xFF00FF);
        // Graphics.DrawLine(colorBuffer, 50, 200, 50, 300, 0xFF00FF);
        // Graphics.DrawLine(colorBuffer, 100, 200, 150, 300, 0xFF00FF);

        fixed(void* pixelDataPtr = &colorBuffer.Pixels[0])
            glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, colorBuffer.Width, colorBuffer.Height, GL_RGBA, GL_UNSIGNED_BYTE, pixelDataPtr);

        // Drawing the textured quad.
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