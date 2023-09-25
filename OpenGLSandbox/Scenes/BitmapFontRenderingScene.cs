using System.Numerics;
using System.Text;
using static GL46;
using static OpenGLSandbox.Utils_GL;
using BmFont;

namespace OpenGLSandbox;


public sealed class TgaImage
{
    private string PathToFile { get; }
    
    public TgaImage(string pathToFile)
    {
        PathToFile = pathToFile;
    }

    public unsafe void UploadToGpu()
    {
        var width = 0;
        var height = 0;
        byte[] pixels;
        using (BinaryReader reader = new BinaryReader(File.Open(PathToFile, FileMode.Open)))
        {
            // Read the TGA header
            byte[] header = reader.ReadBytes(18);
            int imageType = header[2];

            if (imageType != 2 && imageType != 3)
            {
                throw new Exception("Unsupported TGA format.");
            }

            width = BitConverter.ToInt16(header, 12);
            height = BitConverter.ToInt16(header, 14);
            int bitsPerPixel = header[16];

            // Read the image data
            var dataSize = width * height * (bitsPerPixel / 8);
            pixels = reader.ReadBytes(dataSize);

            Console.WriteLine("Image Type: " + imageType);
            Console.WriteLine("Pixels: " + pixels.Length);
            
            // If the image is stored upside down, you may need to flip it

            fixed (void* ptr = &pixels[0])
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RED, GL_UNSIGNED_BYTE, ptr);
            AssertNoGlError();
        }
    }
}

public sealed unsafe class TextRenderer : IDisposable
{
    struct PerInstanceData
    {
        public Rect PositionRect;
        public Rect GlyphSheetRect;
    }
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_Tex;
    private uint m_PerInstanceBuffer;
    private uint m_ShaderProgram;
    private FontFile m_Font;
    private Dictionary<int, FontChar> m_IdToGlyphTable = new();
    private float ScaleW;
    private float ScaleH;
    
    public TextRenderer()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        m_Vao = vao;

        uint vbo;
        glGenBuffers(1, &vbo);
        AssertNoGlError();
        m_Vbo = vbo;
        
        glBindVertexArray(vao);
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        AssertNoGlError();

        var texturedQuad = new TexturedQuad();
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(TexturedQuad)), &texturedQuad, GL_STATIC_DRAW);
        AssertNoGlError();

        uint positionAttribLocation = 0;
        glVertexAttribPointer(positionAttribLocation, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset(0));
        AssertNoGlError();
        glEnableVertexAttribArray(positionAttribLocation);
        AssertNoGlError();

        uint texCoordsAttribLocation = 1;
        glVertexAttribPointer(texCoordsAttribLocation, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords)));
        AssertNoGlError();
        glEnableVertexAttribArray(texCoordsAttribLocation);
        AssertNoGlError();

        uint perInstanceBuffer;
        glGenBuffers(1, &perInstanceBuffer);
        AssertNoGlError();
        m_PerInstanceBuffer = perInstanceBuffer;

        glBindBuffer(GL_ARRAY_BUFFER, perInstanceBuffer);
        AssertNoGlError();
        
        var maxCharCount = 256;
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(maxCharCount * sizeof(PerInstanceData)), (void*)0, GL_STREAM_DRAW);
        AssertNoGlError();

        uint positionRectAttribLocation = 2;
        glVertexAttribPointer(positionRectAttribLocation, 4, GL_FLOAT, false, sizeof(PerInstanceData), Offset<PerInstanceData>(nameof(PerInstanceData.PositionRect)));
        glEnableVertexAttribArray(positionRectAttribLocation);
        glVertexAttribDivisor(positionRectAttribLocation, 1);
        AssertNoGlError();
        
        // Location in the glyph sheet
        uint glyphSheetRectAttribLocation = 3;
        glVertexAttribPointer(glyphSheetRectAttribLocation, 4, GL_FLOAT, false, sizeof(PerInstanceData), Offset<PerInstanceData>(nameof(PerInstanceData.GlyphSheetRect)));
        glEnableVertexAttribArray(glyphSheetRectAttribLocation);
        glVertexAttribDivisor(glyphSheetRectAttribLocation, 1);
        AssertNoGlError();
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
            .WithFragmentShader("Assets/tex.frag.glsl")
            .Build();

        glUseProgram(m_ShaderProgram);
        AssertNoGlError();

        var projectionMatrixUniformLocation = GetUniformLocation("u_ProjectionMatrix");
        Console.WriteLine("Projection Matrix Uniform Location: " + projectionMatrixUniformLocation);
        
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, 320f, 0f, 320f, 0.1f, 100f);
        glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, &projectionMatrix.M11);
        AssertNoGlError();
        
        var font = FontLoader.Load("Assets/bitmapfonts/test.fnt");
        foreach (var glyph in font.Chars)
            m_IdToGlyphTable.Add(glyph.ID, glyph);

        ScaleW = font.Common.ScaleW;
        ScaleH = font.Common.ScaleH;

        uint tex;
        glGenTextures(1, &tex);
        AssertNoGlError();
        m_Tex = tex;
        
        glActiveTexture(GL_TEXTURE0);
        AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, tex);
        AssertNoGlError();
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        AssertNoGlError();

        var image = new TgaImage("Assets/bitmapfonts/test_0.tga");
        image.UploadToGpu();
    }
    
    public void RenderText(int x, int y, ReadOnlySpan<char> text)
    {
        var cursor = new Vector2(x, y);
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        var bufferPtr = glMapBuffer(GL_ARRAY_BUFFER, GL_WRITE_ONLY);
        var buffer = new Span<PerInstanceData>(bufferPtr, text.Length);
        var i = 0;
        foreach (var c in text)
        {
            var glyph = GetGlyph(c);
            var xPos = cursor.X + glyph.XOffset;
            var yPos = cursor.Y;// - glyph.YOffset;
            var uOffset = glyph.X / ScaleW;
            var vOffset = glyph.Y / ScaleH;
            var uScale = glyph.Width / ScaleW;
            var vScale = glyph.Height / ScaleH;
            //Console.WriteLine($"{c}: ({uOffset}, {vOffset})\t({uScale}, {vScale})");
            buffer[i] = new PerInstanceData
            {
                PositionRect = new Rect(xPos, yPos, glyph.Width, glyph.Height),
                GlyphSheetRect = new Rect(uOffset, vOffset, uScale, vScale)
            };
            i++;
            cursor.X += glyph.XAdvance;
        }

        glUnmapBuffer(GL_ARRAY_BUFFER);
        
        glDrawArraysInstanced(GL_TRIANGLES, 0, 6, text.Length);
    }

    private int GetUniformLocation(string uniformName)
    {
        var uniformNameAsAsciiBytes = Encoding.ASCII.GetBytes(uniformName);
        int uniformLocation;
        fixed(byte* ptr = &uniformNameAsAsciiBytes[0])
            uniformLocation = glGetUniformLocation(m_ShaderProgram, ptr);
        AssertNoGlError();
        return uniformLocation;
    }
    
    public void Dispose()
    {
        fixed (uint* vao = &m_Vao)
            glDeleteVertexArrays(1, vao);
        
        fixed (uint* vbo = &m_Vbo)
            glDeleteBuffers(1, vbo);
        
        fixed (uint* buffer = &m_PerInstanceBuffer)
            glDeleteBuffers(1, buffer);
        
        fixed (uint* tex = &m_Tex)
            glDeleteTextures(1, tex);
        
        glDeleteProgram(m_ShaderProgram);
    }

    private FontChar GetGlyph(char c)
    {
        var id = (int)c;
        if (m_IdToGlyphTable.TryGetValue(id, out var glyph))
            return glyph;
        
        Console.WriteLine($"Could not find glyph for char '{c}'");
        return new FontChar();
    }
} 

public sealed class BitmapFontRenderingScene : IScene
{
    private TextRenderer TextRenderer { get; set; }
    
    public void Load()
    {
        TextRenderer = new TextRenderer();
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        TextRenderer.RenderText(20, 200, "Hello world!");
        glFlush();
    }

    public void Unload()
    {
        TextRenderer.Dispose();   
    }
}