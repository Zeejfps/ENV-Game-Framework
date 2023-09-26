using System.Numerics;
using System.Text;
using BmFont;

namespace OpenGLSandbox;

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
    private readonly int m_Base;
    private readonly int m_LineHeight;
    private readonly Random m_Random = new Random();
    
    public TextRenderer()
    {
        uint vao;
        GL46.glGenVertexArrays(1, &vao);
        Utils_GL.AssertNoGlError();
        m_Vao = vao;

        uint vbo;
        GL46.glGenBuffers(1, &vbo);
        Utils_GL.AssertNoGlError();
        m_Vbo = vbo;
        
        GL46.glBindVertexArray(vao);
        Utils_GL.AssertNoGlError();
        
        GL46.glBindBuffer(GL46.GL_ARRAY_BUFFER, vbo);
        Utils_GL.AssertNoGlError();

        var texturedQuad = new TexturedQuad();
        GL46.glBufferData(GL46.GL_ARRAY_BUFFER, new IntPtr(sizeof(TexturedQuad)), &texturedQuad, GL46.GL_STATIC_DRAW);
        Utils_GL.AssertNoGlError();

        uint positionAttribLocation = 0;
        GL46.glVertexAttribPointer(positionAttribLocation, 2, GL46.GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Utils_GL.Offset(0));
        Utils_GL.AssertNoGlError();
        GL46.glEnableVertexAttribArray(positionAttribLocation);
        Utils_GL.AssertNoGlError();

        uint texCoordsAttribLocation = 1;
        GL46.glVertexAttribPointer(texCoordsAttribLocation, 2, GL46.GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Utils_GL.Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords)));
        Utils_GL.AssertNoGlError();
        GL46.glEnableVertexAttribArray(texCoordsAttribLocation);
        Utils_GL.AssertNoGlError();

        uint perInstanceBuffer;
        GL46.glGenBuffers(1, &perInstanceBuffer);
        Utils_GL.AssertNoGlError();
        m_PerInstanceBuffer = perInstanceBuffer;

        GL46.glBindBuffer(GL46.GL_ARRAY_BUFFER, perInstanceBuffer);
        Utils_GL.AssertNoGlError();
        
        var maxCharCount = 256;
        GL46.glBufferData(GL46.GL_ARRAY_BUFFER, new IntPtr(maxCharCount * sizeof(PerInstanceData)), (void*)0, GL46.GL_STREAM_DRAW);
        Utils_GL.AssertNoGlError();

        uint positionRectAttribLocation = 2;
        GL46.glVertexAttribPointer(positionRectAttribLocation, 4, GL46.GL_FLOAT, false, sizeof(PerInstanceData), Utils_GL.Offset<PerInstanceData>(nameof(PerInstanceData.PositionRect)));
        GL46.glEnableVertexAttribArray(positionRectAttribLocation);
        GL46.glVertexAttribDivisor(positionRectAttribLocation, 1);
        Utils_GL.AssertNoGlError();
        
        // Location in the glyph sheet
        uint glyphSheetRectAttribLocation = 3;
        GL46.glVertexAttribPointer(glyphSheetRectAttribLocation, 4, GL46.GL_FLOAT, false, sizeof(PerInstanceData), Utils_GL.Offset<PerInstanceData>(nameof(PerInstanceData.GlyphSheetRect)));
        GL46.glEnableVertexAttribArray(glyphSheetRectAttribLocation);
        GL46.glVertexAttribDivisor(glyphSheetRectAttribLocation, 1);
        Utils_GL.AssertNoGlError();
        
        // NOTE(Zee): I am going to make color a per instance variable on purpose
        // This allows us to color each letter differently instead of the whole text
        uint colorRectAttribLocation = 4;
        GL46.glVertexAttribPointer(colorRectAttribLocation, 4, GL46.GL_FLOAT, false, sizeof(PerInstanceData), Utils_GL.Offset<PerInstanceData>(nameof(PerInstanceData.Color)));
        GL46.glEnableVertexAttribArray(colorRectAttribLocation);
        GL46.glVertexAttribDivisor(colorRectAttribLocation, 1);
        Utils_GL.AssertNoGlError();
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
            .WithFragmentShader("Assets/Shaders/bmpfont.frag.glsl")
            .Build();

        GL46.glUseProgram(m_ShaderProgram);
        Utils_GL.AssertNoGlError();

        var projectionMatrixUniformLocation = GetUniformLocation("u_ProjectionMatrix");
        Console.WriteLine("Projection Matrix Uniform Location: " + projectionMatrixUniformLocation);
        
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, 640f, 0f, 640f, 0.1f, 100f);
        GL46.glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, &projectionMatrix.M11);
        Utils_GL.AssertNoGlError();
        
        var font = FontLoader.Load("Assets/bitmapfonts/test.fnt");
        foreach (var glyph in font.Chars)
            m_IdToGlyphTable.Add(glyph.ID, glyph);

        m_ScaleW = font.Common.ScaleW;
        m_ScaleH = font.Common.ScaleH;
        m_LineHeight = font.Common.LineHeight;
        m_Base = font.Common.Base;

        uint tex;
        GL46.glGenTextures(1, &tex);
        Utils_GL.AssertNoGlError();
        m_Tex = tex;
        
        GL46.glActiveTexture(GL46.GL_TEXTURE0);
        Utils_GL.AssertNoGlError();
        GL46.glBindTexture(GL46.GL_TEXTURE_2D, tex);
        Utils_GL.AssertNoGlError();
        
        GL46.glTexParameteri(GL46.GL_TEXTURE_2D, GL46.GL_TEXTURE_MAX_LEVEL, 0);
        GL46.glTexParameteri(GL46.GL_TEXTURE_2D, GL46.GL_TEXTURE_WRAP_S, GL46.GL_REPEAT);
        GL46.glTexParameteri(GL46.GL_TEXTURE_2D, GL46.GL_TEXTURE_WRAP_T, GL46.GL_REPEAT);
        GL46.glTexParameteri(GL46.GL_TEXTURE_2D, GL46.GL_TEXTURE_MIN_FILTER, GL46.GL_NEAREST);
        GL46.glTexParameteri(GL46.GL_TEXTURE_2D, GL46.GL_TEXTURE_MAG_FILTER, GL46.GL_NEAREST);
        Utils_GL.AssertNoGlError();

        var image = new TgaImage("Assets/bitmapfonts/test_0.tga");
        image.UploadToGpu();
    }

    public int LineHeight => m_LineHeight;

    public void RenderText(int x, int y, Color color, ReadOnlySpan<char> text)
    {
        var cursor = new Vector2(x, y);
        GL46.glBindBuffer(GL46.GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        var bufferPtr = GL46.glMapBuffer(GL46.GL_ARRAY_BUFFER, GL46.GL_WRITE_ONLY);
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
            
            var offsetFromTop = glyph.YOffset - (m_Base - glyph.Height);
            var yPos = cursor.Y - offsetFromTop;
            
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

        GL46.glUnmapBuffer(GL46.GL_ARRAY_BUFFER);
        
        GL46.glDrawArraysInstanced(GL46.GL_TRIANGLES, 0, 6, text.Length);
    }

    private int GetUniformLocation(string uniformName)
    {
        var uniformNameAsAsciiBytes = Encoding.ASCII.GetBytes(uniformName);
        int uniformLocation;
        fixed(byte* ptr = &uniformNameAsAsciiBytes[0])
            uniformLocation = GL46.glGetUniformLocation(m_ShaderProgram, ptr);
        Utils_GL.AssertNoGlError();
        return uniformLocation;
    }
    
    public void Dispose()
    {
        fixed (uint* vao = &m_Vao)
            GL46.glDeleteVertexArrays(1, vao);
        
        fixed (uint* vbo = &m_Vbo)
            GL46.glDeleteBuffers(1, vbo);
        
        fixed (uint* buffer = &m_PerInstanceBuffer)
            GL46.glDeleteBuffers(1, buffer);
        
        fixed (uint* tex = &m_Tex)
            GL46.glDeleteTextures(1, tex);
        
        GL46.glDeleteProgram(m_ShaderProgram);
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