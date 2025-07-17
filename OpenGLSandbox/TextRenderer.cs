using System.Numerics;
using System.Text;
using EasyGameFramework.GUI;
using ZGF.BMFontModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public sealed unsafe class TextRenderer
{
    
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_Tex;
    private uint m_PerInstanceBuffer;
    private uint m_ShaderProgram;
    private Dictionary<int, FontChar> m_IdToGlyphTable = new();
    private float m_ScaleW;
    private float m_ScaleH;
    private int m_Base;
    private int m_LineHeight;

    private const int MaxGlyphCount = 50000;

    public int LineHeight => m_LineHeight;

    private int m_GlyphCount;

    public void DrawText(Rect screenRect, TextStyle style, ReadOnlySpan<char> text)
    {
        var color = style.Color;
        var horizontalAlignment = style.HorizontalTextAlignment;
        var verticalAlignment = style.VerticalTextAlignment;
        var textWidth = CalculateWidth(text);
        var textHeight = CalculateHeight(text);
        var leftPadding = 0f;
        var bottomPadding = 0f;
        
        switch (horizontalAlignment)
        {
            case TextAlignment.Start:
                leftPadding = 0f;
                break;
            case TextAlignment.Center:
                leftPadding = MathF.Floor((screenRect.Width - textWidth) * 0.5f);
                break;
            case TextAlignment.End:
                leftPadding = MathF.Floor(screenRect.Width - textWidth);
                break;
            case TextAlignment.Justify:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (verticalAlignment)
        {
            case TextAlignment.Start:
                bottomPadding = screenRect.Height - textHeight;
                break;
            case TextAlignment.Center:
                bottomPadding = MathF.Floor(screenRect.Height - textHeight) * 0.5f;
                break;
            case TextAlignment.End:
                bottomPadding = 0f;
                break;
            case TextAlignment.Justify:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var x = (int)(screenRect.X + leftPadding);
        var y = (int)(screenRect.Y + bottomPadding);
        DrawText(x, y, color, text);
    }

    private int CalculateHeight(ReadOnlySpan<char> text)
    {
        var h = 0;
        foreach (var c in text)
        {
            if (TryGetGlyph(c, out var glyph))
            {
                if (glyph.Height > h)
                    h = glyph.Height;
            }
        }
        return h;
    }
    
    private int CalculateWidth(ReadOnlySpan<char> text)
    {
        var textWidthInPixels = 0;
        foreach (var c in text)
        {
            if (!TryGetGlyph(c, out var glyph))
                continue;
            textWidthInPixels += glyph.XOffset + glyph.XAdvance;
        }

        return textWidthInPixels;
    }
    
    public void DrawText(int x, int y, Color color, ReadOnlySpan<char> text)
    {
        //Console.WriteLine(text.Length);
        var cursor = new Vector2(x, y);
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        // NOTE(Zee): This is working on an orphaned buffer so we don't need synchronization
        var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, new IntPtr(m_GlyphCount * sizeof(Glyph)), new IntPtr(text.Length * sizeof(Glyph)),  GL_MAP_WRITE_BIT | GL_MAP_UNSYNCHRONIZED_BIT);
        AssertNoGlError();
        var buffer = new Span<Glyph>(bufferPtr, text.Length);
        var i = 0;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                cursor.X = x;
                cursor.Y -= m_LineHeight;
                i++;
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
            buffer[i] = new Glyph
            {
                ScreenRect = new Rect(xPos, yPos, glyph.Width, glyph.Height),
                TextureRect = new Rect(uOffset, vOffset, uScale, vScale),
                Color = color
            };
            i++;
            cursor.X += glyph.XAdvance;
        }

        glUnmapBuffer(GL_ARRAY_BUFFER);
        m_GlyphCount += i;
    }

    public void Render()
    {
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glUseProgram(m_ShaderProgram);
        glBindVertexArray(m_Vao);
        glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_GlyphCount);
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
        m_IdToGlyphTable.Clear();
        
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
        if (TryGetGlyph(c, out var glyph))
            return glyph;
        
        Console.WriteLine($"Could not find glyph for char '{c}'");
        return new FontChar();
    }

    private bool TryGetGlyph(char c, out FontChar glyph)
    {
        var id = (int)c;
        return m_IdToGlyphTable.TryGetValue(id, out glyph);
    }

    public void Clear()
    {
        m_GlyphCount = 0;
        // NOTE(Zee): Orphan the buffer
        glBindBuffer(GL_ARRAY_BUFFER, m_PerInstanceBuffer);
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(MaxGlyphCount * sizeof(Glyph)), (void*)0, GL_DYNAMIC_DRAW);
    }

    public void Load()
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
        
        var maxCharCount = MaxGlyphCount;
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(maxCharCount * sizeof(Glyph)), (void*)0, GL_DYNAMIC_DRAW);
        AssertNoGlError();

        uint positionRectAttribLocation = 2;
        glVertexAttribPointer(positionRectAttribLocation, 4, GL_FLOAT, false, sizeof(Glyph), Offset<Glyph>(nameof(Glyph.ScreenRect)));
        glEnableVertexAttribArray(positionRectAttribLocation);
        glVertexAttribDivisor(positionRectAttribLocation, 1);
        AssertNoGlError();
        
        // Location in the glyph sheet
        uint glyphSheetRectAttribLocation = 3;
        glVertexAttribPointer(glyphSheetRectAttribLocation, 4, GL_FLOAT, false, sizeof(Glyph), Offset<Glyph>(nameof(Glyph.TextureRect)));
        glEnableVertexAttribArray(glyphSheetRectAttribLocation);
        glVertexAttribDivisor(glyphSheetRectAttribLocation, 1);
        AssertNoGlError();
        
        // NOTE(Zee): I am going to make color a per instance variable on purpose
        // This allows us to color each letter differently instead of the whole text
        uint colorRectAttribLocation = 4;
        glVertexAttribPointer(colorRectAttribLocation, 4, GL_FLOAT, false, sizeof(Glyph), Offset<Glyph>(nameof(Glyph.Color)));
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
        var projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, 640f, 0f, 640f, 0.1f, 100f);
        glUniformMatrix4fv(projectionMatrixUniformLocation, 1, false, &projectionMatrix.M11);
        AssertNoGlError();
        
        var font = BMFontFileUtils.DeserializeFromXmlFile("Assets/bitmapfonts/Segoe UI.fnt");
        foreach (var glyph in font.Chars)
            m_IdToGlyphTable.Add(glyph.ID, glyph);

        m_ScaleW = font.Common.ScaleW;
        m_ScaleH = font.Common.ScaleH;
        m_LineHeight = font.Common.LineHeight;
        m_Base = font.Common.Base;

        uint tex;
        glGenTextures(1, &tex);
        AssertNoGlError();
        m_Tex = tex;
        
        glActiveTexture(GL_TEXTURE0);
        AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, tex);
        AssertNoGlError();
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        AssertNoGlError();

        var image = new TgaImage("Assets/bitmapfonts/Segoe UI_0.tga");
        image.UploadToGpu();
    }
}