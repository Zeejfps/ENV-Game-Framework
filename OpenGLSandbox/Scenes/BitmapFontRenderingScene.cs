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

            uint uploadBufferId;
            glGenBuffers(1, &uploadBufferId);
            AssertNoGlError();
            glBindBuffer(GL_PIXEL_UNPACK_BUFFER, uploadBufferId);
            AssertNoGlError();

            glBufferData(GL_PIXEL_UNPACK_BUFFER, new IntPtr(dataSize), (void*)0, GL_STATIC_DRAW);
            AssertNoGlError();
            
            var ptrToBuffer = glMapBuffer(GL_PIXEL_UNPACK_BUFFER, GL_WRITE_ONLY);
            AssertNoGlError();
            
            var buffer = new Span<byte>(ptrToBuffer, dataSize);
            reader.Read(buffer);
            
            glUnmapBuffer(GL_PIXEL_UNPACK_BUFFER);
            AssertNoGlError();

            //Console.WriteLine("Image Type: " + imageType);
            //Console.WriteLine("Pixels: " + buffer.Length);
            
            // If the image is stored upside down, you may need to flip it

            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RED, GL_UNSIGNED_BYTE, Offset(0));
            AssertNoGlError();
            
            glDeleteBuffers(1, &uploadBufferId);
        }
    }
}

public sealed unsafe class TextRenderer : IDisposable
{
    struct PerInstanceData
    {
        public Rect PositionRect;
        public Rect GlyphSheetRect;
        public Color Color;
    }
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_Tex;
    private uint m_PerInstanceBuffer;
    private uint m_ShaderProgram;
    private FontFile m_Font;
    private Dictionary<int, FontChar> m_IdToGlyphTable = new();
    private readonly float m_ScaleW;
    private readonly float m_ScaleH;
    private readonly int m_LineHeight;
    private readonly Random m_Random = new Random();
    
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
        
        // NOTE(Zee): I am going to make color a per instance variable on purpose
        // This allows us to color each letter differently instead of the whole text
        uint colorRectAttribLocation = 4;
        glVertexAttribPointer(colorRectAttribLocation, 4, GL_FLOAT, false, sizeof(PerInstanceData), Offset<PerInstanceData>(nameof(PerInstanceData.Color)));
        glEnableVertexAttribArray(colorRectAttribLocation);
        glVertexAttribDivisor(colorRectAttribLocation, 1);
        AssertNoGlError();
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
            .WithFragmentShader("Assets/Shaders/bmpfont.frag.glsl")
            .Build();

        glUseProgram(m_ShaderProgram);
        AssertNoGlError();

        var projectionMatrixUniformLocation = GetUniformLocation("u_ProjectionMatrix");
        Console.WriteLine("Projection Matrix Uniform Location: " + projectionMatrixUniformLocation);
        
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, 640f, 0f, 640f, 0.1f, 100f);
        glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, &projectionMatrix.M11);
        AssertNoGlError();
        
        var font = FontLoader.Load("Assets/bitmapfonts/test.fnt");
        foreach (var glyph in font.Chars)
            m_IdToGlyphTable.Add(glyph.ID, glyph);

        m_ScaleW = font.Common.ScaleW;
        m_ScaleH = font.Common.ScaleH;
        m_LineHeight = font.Common.LineHeight;

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

    public int LineHeight => m_LineHeight;

    public void RenderText(int x, int y, Color color, ReadOnlySpan<char> text)
    {
        var cursor = new Vector2(x, y);
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        var bufferPtr = glMapBuffer(GL_ARRAY_BUFFER, GL_WRITE_ONLY);
        var buffer = new Span<PerInstanceData>(bufferPtr, text.Length);
        var i = 0;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                cursor.X = x;
                cursor.Y -= m_LineHeight;
                continue;
            }
            
            var glyph = GetGlyph(c);
            var xPos = cursor.X + glyph.XOffset;
            var yPos = cursor.Y - (glyph.YOffset - (m_LineHeight - glyph.Height));
            
            var uOffset = glyph.X / m_ScaleW;
            var vOffset = glyph.Y / m_ScaleH;
            var uScale = glyph.Width / m_ScaleW;
            var vScale = glyph.Height / m_ScaleH;
            //Console.WriteLine($"{c}: ({uOffset}, {vOffset})\t({uScale}, {vScale})");
            buffer[i] = new PerInstanceData
            {
                PositionRect = new Rect(xPos, yPos, glyph.Width, glyph.Height),
                GlyphSheetRect = new Rect(uOffset, vOffset, uScale, vScale),
                Color = color
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
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_DST_ALPHA);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        var color = Color.FromHex(0xFF0045, 1f);
        TextRenderer.RenderText(20, 200, color,"Hello world!\nAnd this is a brand new\nline?!");
        TextRenderer.RenderText(50, 300, Color.FromHex(0x0F0f6, 1f),"This is more text");
        TextRenderer.RenderText(200, 240, Color.FromHex(0x2f8777, 1f),"This is EVEN, MORE, perhaps, MOST,\ntext EVER!!!");
        TextRenderer.RenderText(0, 0, Color.FromHex(0x2f8777, 1f),"Should align with bottom left corner");
        TextRenderer.RenderText(0, 640 - TextRenderer.LineHeight, Color.FromHex(0x2f8777, 1f),"Should align with top left corner");
        glFlush();
    }

    public void Unload()
    {
        TextRenderer.Dispose();   
    }
}