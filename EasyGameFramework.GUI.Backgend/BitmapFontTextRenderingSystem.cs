using System.Numerics;
using BmFont;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGLSandbox;

public readonly struct BmpFontFile
{
    public string FontName { get; init; }
    public string PathToFile { get; init; }
}

public sealed unsafe class BitmapFontTextRenderer : ITextRenderer
{
    private readonly IWindow m_Window;
    
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    
    private readonly Dictionary<string, BmpFontRenderer> m_FontNameToFontRendererTable = new();
    
    public BitmapFontTextRenderer(IWindow window)
    {
        m_Window = window;
    }

    public void Load(params BmpFontFile[] files)
    {
        foreach (var fontFile in files)
        {
            var renderer = BmpFontRenderer.Init(fontFile.PathToFile);
            m_FontNameToFontRendererTable.Add(fontFile.FontName, renderer);
        }
        
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
            .WithFragmentShader("Assets/Shaders/bmpfont.frag.glsl")
            .Build();

        m_ProjectionMatrixUniformLocation = GetUniformLocation(m_ShaderProgram, "u_ProjectionMatrix");
        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);
    }

    public void Unload()
    {
        foreach (var renderer in m_FontNameToFontRendererTable.Values)
            renderer.Dispose();
        m_FontNameToFontRendererTable.Clear();
        
        glDeleteProgram(m_ShaderProgram);
        AssertNoGlError();
    }

    public void Update()
    {
        foreach (var renderer in m_FontNameToFontRendererTable.Values)
            renderer.Update();
                     
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                
        glUseProgram(m_ShaderProgram);
        AssertNoGlError();

        m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);
        fixed (float* ptr = &m_ProjectionMatrix.M11)
            glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        foreach (var renderer in m_FontNameToFontRendererTable.Values)
            renderer.Render();
    }
    
    public IRenderedText Render(string text, Rect screenRect, TextStyle style)
    {
        var fontFamily = style.FontFamily;
        if (m_FontNameToFontRendererTable.TryGetValue(fontFamily, out var font))
            return new RenderedTextImpl(font, screenRect, style, text);
        
        throw new Exception($"Could not find font with name: {fontFamily}");
    }

    public float CalculateTextWidth(string text, string fontName)
    {
        if (m_FontNameToFontRendererTable.TryGetValue(fontName, out var fontRenderer))
            return fontRenderer.CalculateSize(text, new TextStyle()).Width;
        return 0f;
    }

    public Size CalculateSize(string text, string fontName, TextStyle style)
    {
        if (m_FontNameToFontRendererTable.TryGetValue(fontName, out var fontRenderer))
            return fontRenderer.CalculateSize(text, style);
        return new Size();
    }
}


sealed unsafe class BmpFontRenderer : IDisposable
{
    private readonly uint m_TextureId;
    private readonly FontFile m_FontFile;
    private readonly OpenGlTexturedQuadInstanceRenderer<Glyph> m_TexturedQuadInstanceRenderer;

    private BmpFontRenderer(uint textureId, FontFile fontFile, OpenGlTexturedQuadInstanceRenderer<Glyph> texturedQuadInstanceRenderer)
    {
        m_TextureId = textureId;
        m_FontFile = fontFile;
        m_TexturedQuadInstanceRenderer = texturedQuadInstanceRenderer;
    }

    public FontFile FontFile => m_FontFile;

    public static BmpFontRenderer Init(string pathToFontFile)
    {
        var texturedQuadInstanceRenderer = new OpenGlTexturedQuadInstanceRenderer<Glyph>(10_000);
        texturedQuadInstanceRenderer.Load();
        
        uint textureId;
        glGenTextures(1, &textureId);
        AssertNoGlError();
            
        glActiveTexture(GL_TEXTURE0);
        AssertNoGlError();
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();
        
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_REPEAT);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_REPEAT);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        AssertNoGlError();
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        AssertNoGlError();

        var fontFileDirectory = Path.GetDirectoryName(pathToFontFile);
        var fontFile = FontLoader.Load(pathToFontFile);
        var pageFileName = fontFile.Pages[0].File;
        var image = new TgaImage(Path.Combine(fontFileDirectory, pageFileName));
        image.UploadToGpu();

        return new BmpFontRenderer(textureId, fontFile, texturedQuadInstanceRenderer);
    }
    
    internal void Remove(RenderedGlyphImpl glyph)
    {
        m_TexturedQuadInstanceRenderer.Remove(glyph);
    }

    internal void Add(RenderedGlyphImpl glyph)
    {
        m_TexturedQuadInstanceRenderer.Add(glyph);
    }
    
    public IEnumerable<int> AsCodePoints(string s)
    {
        for(int i = 0; i < s.Length; ++i)
        {
            yield return char.ConvertToUtf32(s, i);
            if(char.IsHighSurrogate(s, i))
                i++;
        }
    }

    private void ReleaseUnmanagedResources()
    {
        fixed(uint* ptr = &m_TextureId)
            glDeleteTextures(1, ptr);
        AssertNoGlError();    
        
        m_TexturedQuadInstanceRenderer.Unload();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~BmpFontRenderer()
    {
        ReleaseUnmanagedResources();
    }

    public void Update()
    {
    }

    public void Render()
    {
        glBindTexture(GL_TEXTURE_2D, m_TextureId);
        AssertNoGlError();
        m_TexturedQuadInstanceRenderer.Render();
    }

    public Size CalculateSize(string text, TextStyle style)
    {
        var font = FontFile;
        var textWidthInPixels = 0;
        var textHeightInPixels = 0;
        foreach (var c in AsCodePoints(text))
        {
            if (!font.TryGetFontChar(c, out var glyph))
                continue;
            
            textWidthInPixels += glyph.XOffset + glyph.XAdvance;
            
            var glyphHeight = glyph.Height;
            if (glyphHeight > textHeightInPixels)
                textHeightInPixels = glyphHeight;
        }
        
        return new Size(textWidthInPixels, font.Common.Base);
    }
}

sealed class RenderedTextImpl : IRenderedText
{
    private Rect m_ScreenRect;
    public Rect ScreenRect
    {
        get => m_ScreenRect;
        set
        {
            m_ScreenRect = value;
            LayoutGlyphs();
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

    public Rect Bounds { get; private set; }
    public int GlyphCount => m_Glyphs.Count;
    
    public IRenderedGlyph GetGlyph(int index)
    {
        return m_Glyphs[index];
    }

    public IEnumerable<IRenderedGlyph> Glyphs => m_Glyphs;

    private readonly string m_Text;
    private readonly List<RenderedGlyphImpl> m_Glyphs = new();
    private BmpFontRenderer m_FontRenderer;
    
    public RenderedTextImpl(BmpFontRenderer fontRenderer, Rect screenRect, TextStyle style, string text)
    {
        m_ScreenRect = screenRect;
        m_Style = style;
        m_Text = text;
        m_FontRenderer = fontRenderer;
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
            m_FontRenderer.Add(glyph);
            m_Glyphs.Add(glyph);
        }
    }

    private void LayoutGlyphs()
    {
        var textSize = m_FontRenderer.CalculateSize(m_Text, Style);
        var position = CalculatePosition(ScreenRect, Style, textSize);
        Bounds = new Rect(position.X, position.Y, textSize.Width, textSize.Height);
        var cursor = new Vector2(position.X, position.Y);
        var color = Style.Color;
        var text = m_Text;
        var fontFile = m_FontRenderer.FontFile;
        var baseOffset = fontFile.Common.Base;
        var scaleW = (float)fontFile.Common.ScaleW;
        var scaleH = (float)fontFile.Common.ScaleH;
        var lineHeight = (float)fontFile.Common.LineHeight;
        
        var i = 0;
        foreach (var codePoint in AsCodePoints(text))
        {
            if (codePoint == '\n')
            {
                cursor.X = position.X;
                cursor.Y -= lineHeight;
                continue;
            }
                
            if (!fontFile.TryGetFontChar(codePoint, out var fontChar))
                continue;
                
            var xPos = cursor.X + fontChar.XOffset;

            var fontScale = Style.FontScale;
            var offsetFromTop = fontChar.YOffset - (baseOffset - fontChar.Height);
            var yPos = cursor.Y - offsetFromTop * fontScale;
            var width = fontChar.Width * fontScale;
            var height = fontChar.Height * fontScale;
            
            var uOffset = fontChar.X / scaleW;
            var vOffset = fontChar.Y / scaleH;
            var uScale = fontChar.Width / scaleW;
            var vScale = fontChar.Height / scaleH;

            var glyph = m_Glyphs[i];
            glyph.ScreenRect = new Rect(xPos, yPos, width, height);
            glyph.TextureRect = new Rect(uOffset, vOffset, uScale, vScale);
            glyph.Color = color;
            
            //Console.WriteLine($"{c}: ({uOffset}, {vOffset})\t({uScale}, {vScale})");
            cursor.X += fontChar.XAdvance * fontScale;
            i++;
        }
    }
    
    public Vector2 CalculatePosition(Rect screenRect, TextStyle style, Size textSize)
    {
        var horizontalAlignment = style.HorizontalTextAlignment;
        var verticalAlignment = style.VerticalTextAlignment;
        var textWidth = textSize.Width * style.FontScale;
        var textHeight = textSize.Height * style.FontScale;
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
    
    private IEnumerable<int> AsCodePoints(string s)
    {
        for(int i = 0; i < s.Length; ++i)
        {
            yield return char.ConvertToUtf32(s, i);
            if(char.IsHighSurrogate(s, i))
                i++;
        }
    }

    private void DestroyAllGlyphs()
    {
        foreach (var glyph in m_Glyphs)
            m_FontRenderer.Remove(glyph);
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
sealed class RenderedGlyphImpl : IRenderedGlyph, IEntity<Glyph>
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

    public event Action<IEntity<Glyph>>? BecameDirty;
    
    public void LoadComponent(ref Glyph glyph)
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