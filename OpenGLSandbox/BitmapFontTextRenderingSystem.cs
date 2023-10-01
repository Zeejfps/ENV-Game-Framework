using System.Numerics;
using BmFont;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class BitmapFontTextRenderer : ITextRenderer
{
    private const uint MaxGlyphCount = 20000;

    private readonly IWindow m_Window;
    private readonly string m_PathToFontFile;
    private readonly Dictionary<int, FontChar> m_IdToGlyphTable = new();
    
    private uint m_ShaderProgram;
    private uint m_Texture;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    private float m_ScaleW;
    private float m_ScaleH;
    private int m_Base;
    private int m_LineHeight;

    private TexturedQuadInstancedRenderingSystem<Glyph> m_Renderer;
        
    public BitmapFontTextRenderer(IWindow window, string pathToFontFile)
    {
        m_Window = window;
        m_PathToFontFile = pathToFontFile;
        m_Renderer = new TexturedQuadInstancedRenderingSystem<Glyph>(MaxGlyphCount);
    }

    public int LineHeight => m_LineHeight;
    public int Base => m_Base;
    public float ScaleW => m_ScaleW;
    public float ScaleH => m_ScaleH;

    public void Load()
    {
        m_Renderer.Load();
        
        uint id;
        glGenTextures(1, &id);
        AssertNoGlError();
        m_Texture = id;
            
        glActiveTexture(GL_TEXTURE0);
        AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, m_Texture);
        AssertNoGlError();
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
        AssertNoGlError();

        var font = FontLoader.Load(m_PathToFontFile);
        foreach (var glyph in font.Chars)
            m_IdToGlyphTable.Add(glyph.ID, glyph);

        m_ScaleW = font.Common.ScaleW;
        m_ScaleH = font.Common.ScaleH;
        m_LineHeight = font.Common.LineHeight;
        m_Base = font.Common.Base;

        var imageFileName = font.Pages[0].File;
        var image = new TgaImage($"Assets/bitmapfonts/" + imageFileName);
        image.UploadToGpu();
            
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
            .WithFragmentShader("Assets/Shaders/bmpfont.frag.glsl")
            .Build();

        m_ProjectionMatrixUniformLocation = GetUniformLocation(m_ShaderProgram, "u_ProjectionMatrix");
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);
    }

    public void Unload()
    {
        m_Renderer.Unload();
            
        fixed(uint* ptr = &m_Texture)
            glDeleteTextures(1, ptr);
        AssertNoGlError();    
        
        glDeleteProgram(m_ShaderProgram);
        AssertNoGlError();    
        
        m_IdToGlyphTable.Clear();
    }

    public void Update()
    {
        m_Renderer.Update();
                     
        //Console.WriteLine($"Shader program: {m_ShaderProgram}")
        if (m_Renderer.ItemCount > 0)
        {
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();
            
            fixed (float* ptr = &m_ProjectionMatrix.M11)
                glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
            AssertNoGlError();
                        
            m_Renderer.Render();
        }
    }
    
    public IRenderedText Render(string text, Rect screenRect, TextStyle style)
    {
        return new RenderedTextImpl(this, screenRect, style, text);
    }

    public Vector2 CalculatePosition(Rect screenRect, TextStyle style, string text)
    {
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
        return new Vector2(x, y);
    }
        
    private int CalculateHeight(ReadOnlySpan<char> text)
    {
        // var h = 0;
        // foreach (var c in text)
        // {
        //     if (TryGetGlyph(c, out var glyph))
        //     {
        //         if (glyph.Height > h)
        //             h = glyph.Height;
        //     }
        // }
        return Base;
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
        
    public bool TryGetGlyph(char c, out FontChar glyph)
    {
        var id = (int)c;
        return m_IdToGlyphTable.TryGetValue(id, out glyph);
    }

    internal void Remove(RenderedGlyphImpl glyph)
    {
        m_Renderer.Remove(glyph);
    }

    internal void Add(RenderedGlyphImpl glyph)
    {
        m_Renderer.Add(glyph);
    }
}

public sealed class RenderedTextImpl : IRenderedText
{
    private Rect m_ScreenRect;
    public Rect ScreenRect
    {
        get => m_ScreenRect;
        set
        {
            m_ScreenRect = value;
        }
    }

    private TextStyle m_Style;
    public TextStyle Style
    {
        get => m_Style;
        set
        {
            m_Style = value;
            LayoutGlyphs();
        }
    }

    private readonly string m_Text;
    private readonly List<RenderedGlyphImpl> m_Glyphs = new();
    private readonly BitmapFontTextRenderer m_TextRenderer;

    public RenderedTextImpl(BitmapFontTextRenderer renderer, Rect screenRect, TextStyle style, string text)
    {
        m_TextRenderer = renderer;
        m_ScreenRect = screenRect;
        m_Style = style;
        m_Text = text;
        RegenerateGlyphs();
        LayoutGlyphs();
    }

    private void RegenerateGlyphs()
    {
        DestroyAllGlyphs();
        foreach (var c in m_Text)
        {
            if (c == '\n') continue;
            var glyph = new RenderedGlyphImpl();
            m_TextRenderer.Add(glyph);
            m_Glyphs.Add(glyph);
        }
    }

    private void LayoutGlyphs()
    {
        var position = m_TextRenderer.CalculatePosition(ScreenRect, Style, m_Text);
        var cursor = new Vector2(position.X, position.Y);
        var color = Style.Color;
        var text = m_Text;
        var baseOffset = m_TextRenderer.Base;
        var scaleW = m_TextRenderer.ScaleW;
        var scaleH = m_TextRenderer.ScaleH;
        var lineHeight = m_TextRenderer.LineHeight;
        
        var i = 0;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                cursor.X = position.X;
                cursor.Y -= lineHeight;
                continue;
            }
                
            if (!m_TextRenderer.TryGetGlyph(c, out var fontChar))
                continue;
                
            var xPos = cursor.X + fontChar.XOffset;
            
            var offsetFromTop = fontChar.YOffset - (baseOffset - fontChar.Height);
            var yPos = cursor.Y - offsetFromTop;
            
            var uOffset = fontChar.X / scaleW;
            var vOffset = fontChar.Y / scaleH;
            var uScale = fontChar.Width / scaleW;
            var vScale = fontChar.Height / scaleH;

            var glyph = m_Glyphs[i];
            glyph.ScreenRect = new Rect(xPos, yPos, fontChar.Width, fontChar.Height);
            glyph.TextureRect = new Rect(uOffset, vOffset, uScale, vScale);
            glyph.Color = color;
            
            //Console.WriteLine($"{c}: ({uOffset}, {vOffset})\t({uScale}, {vScale})");
            cursor.X += fontChar.XAdvance;
            i++;
        }
    }

    private void DestroyAllGlyphs()
    {
        foreach (var glyph in m_Glyphs)
            m_TextRenderer.Remove(glyph);
        m_Glyphs.Clear();
    }

    private void ReleaseUnmanagedResources()
    {
        DestroyAllGlyphs();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RenderedTextImpl()
    {
        ReleaseUnmanagedResources();
    }
}

class RenderedGlyphImpl : IRenderedGlyph, IInstancedItem<Glyph>
{
    private Rect m_ScreenRect;
    public Rect ScreenRect
    {
        get => m_ScreenRect;
        set => SetField(ref m_ScreenRect, value);
    }

    private Color m_Color;
    public Color Color
    {
        get => m_Color;
        set => SetField(ref m_Color, value);
    }
    
    public Rect TextureRect { get; set; }

    public event Action<IInstancedItem<Glyph>>? BecameDirty;
    
    public void Update(ref Glyph glyph)
    {
        var rect = ScreenRect;
        rect.X = MathF.Floor(rect.X);
        rect.Y = MathF.Floor(rect.Y);
        rect.Width = MathF.Floor(rect.Width);
        rect.Height = MathF.Floor(rect.Height);

        glyph.ScreenRect = rect;
        glyph.TextureRect = TextureRect;
        glyph.Color = Color;
    }

    private void SetField<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        BecameDirty?.Invoke(this);
    }
}