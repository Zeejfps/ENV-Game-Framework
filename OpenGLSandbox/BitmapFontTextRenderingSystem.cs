using System.Numerics;
using BmFont;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed unsafe class BitmapFontTextRenderingSystem : RenderingSystem<RenderedGlyphImpl>, ITextRenderingSystem
{
    private const uint MaxGlyphCount = 20000;

    private readonly IWindow m_Window;
    private readonly string m_PathToFontFile;
    private readonly Dictionary<int, FontChar> m_IdToGlyphTable = new();

    private uint m_VertexArray;
    private uint m_AttributesBuffer;
    private uint m_InstancesBuffer;
    private uint m_ShaderProgram;
    private uint m_Texture;
    private int m_ProjectionMatrixUniformLocation;
    private Matrix4x4 m_ProjectionMatrix;
    private float m_ScaleW;
    private float m_ScaleH;
    private int m_Base;
    private int m_LineHeight;
        
    public BitmapFontTextRenderingSystem(IWindow window, string pathToFontFile)
    {
        m_Window = window;
        m_PathToFontFile = pathToFontFile;
    }

    public int LineHeight => m_LineHeight;
    public int Base => m_Base;
    public float ScaleW => m_ScaleW;
    public float ScaleH => m_ScaleH;

    public void Load()
    {
        uint id;

        glGenVertexArrays(1, &id);
        AssertNoGlError();
        m_VertexArray = id;
            
        glGenBuffers(1, &id);
        AssertNoGlError();
        m_AttributesBuffer = id;
        
        glGenBuffers(1, &id);
        AssertNoGlError();
        m_InstancesBuffer = id;
            
        glGenTextures(1, &id);
        AssertNoGlError();
        m_Texture = id;
            
        glBindVertexArray(m_VertexArray);
        AssertNoGlError();
            
        SetupAttributesBuffer();
        SetupInstancesBuffer();
            
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
        fixed(uint* ptr = &m_VertexArray)
            glDeleteVertexArrays(1, ptr);
        m_VertexArray = 0;
            
        fixed(uint* ptr = &m_AttributesBuffer)
            glDeleteBuffers(1, ptr);
        m_AttributesBuffer = 0;
            
        fixed(uint* ptr = &m_InstancesBuffer)
            glDeleteBuffers(1, ptr);
        m_InstancesBuffer = 0;
            
        fixed(uint* ptr = &m_Texture)
            glDeleteTextures(1, ptr);
            
        glDeleteProgram(m_ShaderProgram);
            
        m_IdToGlyphTable.Clear();
    }

    public void Update()
    {
        //Console.WriteLine($"Unregistering {m_PanelsToUnregister.Count} panels");
        foreach (var item in m_ItemsToUnregister)
        {
            item.BecameDirty -= Item_OnBecameDirty;
            var id = m_ItemToIndexTable[item];
            m_IdsToFill.Add(id);
            m_IndexToItemTable.Remove(id);
            m_ItemToIndexTable.Remove(item);
        }
        m_ItemsToUnregister.Clear();
            
        //Console.WriteLine($"Registering {m_PanelsToRegister.Count} panels");
        foreach (var item in m_ItemsToRegister)
        {
            item.BecameDirty += Item_OnBecameDirty;
            int id;
            if (m_IdsToFill.Count > 0)
            {
                id = m_IdsToFill.Min;
                //Console.WriteLine($"Reusing an id that needs to be filled. Id: {id}");
                m_IdsToFill.Remove(id);
            }
            else
            {
                id = m_ItemCount;
                //Console.WriteLine($"Assigned a new id. Id: {id}");
                m_ItemCount++;
            }

            m_ItemToIndexTable[item] = id;
            m_IndexToItemTable[id] = item;
                
            m_DirtyItems.Add(id);
        }
        m_ItemsToRegister.Clear();
            
        //Console.WriteLine($"Back filling {m_IdsToFill.Count} ids");
        foreach (var idToFill in m_IdsToFill.Reverse())
        {
            var lastPanelId = m_ItemCount - 1;
            if (idToFill != lastPanelId)
            {
                //Console.WriteLine($"Moving last panel into an id we need to fill. Id: {idToFill}");
                var lastPanel = m_IndexToItemTable[lastPanelId];

                m_IndexToItemTable.Remove(lastPanelId);
                m_IndexToItemTable[idToFill] = lastPanel;
                m_ItemToIndexTable[lastPanel] = idToFill;

                m_DirtyItems.Add(idToFill);
            }
                
            m_ItemCount--;
        }
        m_IdsToFill.Clear();

        var maxIndex = m_DirtyItems.Max;
        //Console.WriteLine($"Max dirty panel index {maxIndex}");

        var maxDirtyGlyphCount = maxIndex + 1;

        m_DirtyItemCount = 0;
        if (m_DirtyItems.Count > 0)
        {
            //Console.WriteLine($"Have dirty items: {m_DirtyItems.Count}");

            glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
            AssertNoGlError();
            var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<Glyph>(maxDirtyGlyphCount), GL_MAP_WRITE_BIT);
            AssertNoGlError();
            var buffer = new Span<Glyph>(bufferPtr, maxDirtyGlyphCount);
            
            foreach (var dirtyItemIndex in m_DirtyItems)
            {
                var srcItem = m_IndexToItemTable[dirtyItemIndex];
                var dstIndex = m_DirtyItemCount;

                if (dirtyItemIndex > m_DirtyItemCount)
                {
                    //Console.WriteLine($"Swaping {panelId} with {dstIndex}");
                    var srcIndex = dirtyItemIndex;

                    var dstPanel = m_IndexToItemTable[dstIndex];
            
                    var dstPanelData = buffer[dstIndex];
                    buffer[srcIndex] = dstPanelData;
            
                    m_IndexToItemTable[srcIndex] = dstPanel;
                    m_ItemToIndexTable[dstPanel] = srcIndex;

                    m_IndexToItemTable[dstIndex] = srcItem;
                    m_ItemToIndexTable[srcItem] = dstIndex;
                }

                srcItem.Update(ref buffer[dstIndex]);
                m_DirtyItemCount++;
            }
            m_DirtyItems.Clear();
            glUnmapBuffer(GL_ARRAY_BUFFER);
        }
            
        //Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
            
        //Console.WriteLine($"Shader program: {m_ShaderProgram}")
        if (m_ItemCount > 0)
        {
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();
            
            fixed (float* ptr = &m_ProjectionMatrix.M11)
                glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
            AssertNoGlError();
                        
            glBindVertexArray(m_VertexArray);
            AssertNoGlError();
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_ItemCount);
            AssertNoGlError();
        }
    }

    private void SetupAttributesBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_AttributesBuffer);
        AssertNoGlError();
            
        var texturedQuad = new TexturedQuad();
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(TexturedQuad)), &texturedQuad, GL_STATIC_DRAW);
        AssertNoGlError();

        uint positionAttribLocation = 0;
        glVertexAttribPointer(
            positionAttribLocation,
            2, 
            GL_FLOAT, 
            false,
            sizeof(TexturedQuad.Vertex), Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.Position))
        );
        AssertNoGlError();
        glEnableVertexAttribArray(positionAttribLocation);
        AssertNoGlError();

        uint texCoordsAttribLocation = 1;
        glVertexAttribPointer(
            texCoordsAttribLocation, 
            2, 
            GL_FLOAT, 
            false, 
            sizeof(TexturedQuad.Vertex), Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords))
        );
        AssertNoGlError();
        glEnableVertexAttribArray(texCoordsAttribLocation);
        AssertNoGlError();
    }

    private void SetupInstancesBuffer()
    {
        glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
        AssertNoGlError();
        
        var maxGlyphCount = MaxGlyphCount;
        glBufferData(GL_ARRAY_BUFFER, SizeOf<Glyph>(maxGlyphCount), (void*)0, GL_DYNAMIC_DRAW);
        AssertNoGlError();

        uint positionRectAttribLocation = 2;
        glVertexAttribPointer(
            positionRectAttribLocation, 
            4, 
            GL_FLOAT, 
            false,
            sizeof(Glyph), Offset<Glyph>(nameof(Glyph.ScreenRect))
        );
        glEnableVertexAttribArray(positionRectAttribLocation);
        glVertexAttribDivisor(positionRectAttribLocation, 1);
        AssertNoGlError();
        
        // Location in the glyph sheet
        uint glyphSheetRectAttribLocation = 3;
        glVertexAttribPointer(
            glyphSheetRectAttribLocation, 
            4, 
            GL_FLOAT, 
            false, 
            sizeof(Glyph), Offset<Glyph>(nameof(Glyph.TextureRect))
        );
        glEnableVertexAttribArray(glyphSheetRectAttribLocation);
        glVertexAttribDivisor(glyphSheetRectAttribLocation, 1);
        AssertNoGlError();
        
        // NOTE(Zee): I am going to make color a per instance variable on purpose
        // This allows us to color each letter differently instead of the whole text
        uint colorRectAttribLocation = 4;
        glVertexAttribPointer(
            colorRectAttribLocation, 
            4, 
            GL_FLOAT, 
            false, 
            sizeof(Glyph), Offset<Glyph>(nameof(Glyph.Color))
        );
        glEnableVertexAttribArray(colorRectAttribLocation);
        glVertexAttribDivisor(colorRectAttribLocation, 1);
        AssertNoGlError();
    }

    public IRenderedText Create(Rect screenRect, TextStyle style, string text)
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
        
    public bool TryGetGlyph(char c, out FontChar glyph)
    {
        var id = (int)c;
        return m_IdToGlyphTable.TryGetValue(id, out glyph);
    }
}

public class RenderedTextImpl : IRenderedText
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
    private readonly BitmapFontTextRenderingSystem m_TextRenderingSystem;

    public RenderedTextImpl(BitmapFontTextRenderingSystem renderingSystem, Rect screenRect, TextStyle style, string text)
    {
        m_TextRenderingSystem = renderingSystem;
        m_ScreenRect = screenRect;
        m_Style = style;
        m_Text = text;
        RegenerateGlyphs();
        LayoutGlyphs();
    }

    private void RegenerateGlyphs()
    {
        foreach (var glyph in m_Glyphs)
            m_TextRenderingSystem.Unregister(glyph);
        m_Glyphs.Clear();
        
        foreach (var c in m_Text)
        {
            if (c == '\n') continue;
            var glyph = new RenderedGlyphImpl();
            m_TextRenderingSystem.Register(glyph);
            m_Glyphs.Add(glyph);
        }
    }

    private void LayoutGlyphs()
    {
        var position = m_TextRenderingSystem.CalculatePosition(ScreenRect, Style, m_Text);
        var cursor = new Vector2(position.X, position.Y);
        var color = Style.Color;
        var text = m_Text;
        var baseOffset = m_TextRenderingSystem.Base;
        var scaleW = m_TextRenderingSystem.ScaleW;
        var scaleH = m_TextRenderingSystem.ScaleH;
        var lineHeight = m_TextRenderingSystem.LineHeight;
        
        var i = 0;
        foreach (var c in text)
        {
            if (c == '\n')
            {
                cursor.X = position.X;
                cursor.Y -= lineHeight;
                continue;
            }
                
            if (!m_TextRenderingSystem.TryGetGlyph(c, out var fontChar))
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
        
    public void Dispose()
    {
        // TODO release managed resources here
    }
}