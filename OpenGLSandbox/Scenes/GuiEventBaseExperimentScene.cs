using System.Numerics;
using System.Text;
using BmFont;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    private readonly PanelRenderingSystem m_PanelRenderingSystem;
    private readonly BitmapFontTextRenderingSystem m_BitmapFontTextRenderingSystem;
    private readonly TextButton[] m_TextButtons;
    
    public GuiEventBaseExperimentScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderingSystem = new PanelRenderingSystem(window);
        m_BitmapFontTextRenderingSystem = new BitmapFontTextRenderingSystem(window);

        var w = 100;
        var h = 100;
        var buttonSize = window.ScreenWidth / (float)w;
        var buttonBorderColor = Color.FromHex(0xff00ff, 1f);
        
        m_TextButtons = new TextButton[w * h];
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                m_TextButtons[i * w + j] = new TextButton(m_PanelRenderingSystem, m_BitmapFontTextRenderingSystem)
                {
                    ScreenRect = new Rect(j*buttonSize, i*buttonSize, buttonSize, buttonSize),
                    BorderColor = buttonBorderColor,
                    BorderRadius = new Vector4(6f, 6f, 6f, 6),
                    BorderSize = BorderSize.All(1f),
                };
            }
        }
    }
    
    public void Load()
    {
        m_PanelRenderingSystem.Load();
        m_BitmapFontTextRenderingSystem.Load();
        foreach (var textButton in m_TextButtons)
        {
            textButton.OnBecameVisible();
        }
    }

    public void Render()
    {
        var mouse = m_InputSystem.Mouse;
        var screenHeight = m_Window.ScreenHeight;
        foreach (var textButton in m_TextButtons)
        {
            var mousePosition = new Vector2(mouse.ScreenX, screenHeight - mouse.ScreenY);
            textButton.IsHovered = textButton.ScreenRect.Contains(mousePosition);
        }
        
        glClear(GL_COLOR_BUFFER_BIT);
        m_PanelRenderingSystem.Update();
        m_BitmapFontTextRenderingSystem.Update();
    }

    public void Unload()
    {
        m_PanelRenderingSystem.Unload();
        m_BitmapFontTextRenderingSystem.Unload();
    }

    interface IText : IDisposable
    {
    }
    
    sealed class TextButton : IPanel
    {
        public Color Color { get; set; }
        public Color BorderColor { get; set; }
        public BorderSize BorderSize { get; set; }
        public Vector4 BorderRadius { get; set; }
        public Rect ScreenRect { get; set; }
        
        public event Action<IPanel>? BecameDirty;

        private bool m_IsHovered;
        public bool IsHovered
        {
            get => m_IsHovered;
            set => SetField(ref m_IsHovered, value);
        }
        
        private readonly IPanelRenderingSystem m_PanelRenderingSystem;
        private readonly ITextRenderingSystem m_TextRenderingSystem;
        
        public TextButton(IPanelRenderingSystem panelRenderer, ITextRenderingSystem textRenderingSystem)
        {
            m_PanelRenderingSystem = panelRenderer;
            m_TextRenderingSystem = textRenderingSystem;
        }

        public void OnBecameVisible()
        {
            m_PanelRenderingSystem.Register(this);
            m_TextRenderingSystem.CreateText(ScreenRect, new TextStyle
            {
                Color = Color.FromHex(0x0022ff, 1f),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }, "K");
        }

        public void OnBecameHidden()
        {
            m_PanelRenderingSystem.Unregister(this);
        }

        public void Update(ref Panel panel)
        {
            panel.ScreenRect = ScreenRect;
            panel.BorderColor = BorderColor;
            panel.BorderSize = BorderSize;
            panel.BorderRadius = BorderRadius;
            panel.Color = IsHovered ? Color.FromHex(0xff00ff, 1f) : Color;
        }

        private void SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            OnBecameDirty();
        }

        private void OnBecameDirty()
        {
            BecameDirty?.Invoke(this);
        }
    }

    interface IFont
    {
        ReadOnlySpan<IGlyph> CreateGlyphs(string text);
    }
    
    class Text
    {
        public Rect ScreenRect { get; set; }
        public string Value { get; set; }
        public TextStyle Style { get; set; }
        
        private readonly ITextRenderingSystem m_TextRenderingSystem;
        private IEnumerable<IGlyph> m_Glyphs = Enumerable.Empty<IGlyph>();

        private IText? m_Text;
        
        private void OnValueChanged(string prevValue, string value)
        {
            if (m_Text != null) m_Text.Dispose();
            m_Text = m_TextRenderingSystem.CreateText(ScreenRect, Style, Value);
        }
    }
    
    

    interface IPanel
    {
        event Action<IPanel> BecameDirty;
        void Update(ref OpenGLSandbox.Panel panel);
    }

    interface IPanelRenderingSystem
    {
        void Register(IPanel panel);
        void Unregister(IPanel panel);
    }

    interface IGlyph
    {
        event Action<IGlyph> BecameDirty;
        void Update(ref Glyph glyph);
    }
    
    interface ITextRenderingSystem
    {
        IText CreateText(Rect screenPosition, TextStyle style, string value);
    }

    sealed unsafe class BitmapFontTextRenderingSystem : RenderingSystem<IGlyph>, ITextRenderingSystem
    {
        private const uint MaxGlyphCount = 20000;

        private readonly IWindow m_Window;
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

        public BitmapFontTextRenderingSystem(IWindow window)
        {
            m_Window = window;
        }

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

            var font = FontLoader.Load("Assets/bitmapfonts/Segoe UI.fnt");
            foreach (var glyph in font.Chars)
                m_IdToGlyphTable.Add(glyph.ID, glyph);

            m_ScaleW = font.Common.ScaleW;
            m_ScaleH = font.Common.ScaleH;
            m_LineHeight = font.Common.LineHeight;
            m_Base = font.Common.Base;
            
            var image = new TgaImage("Assets/bitmapfonts/Segoe UI_0.tga");
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
                sizeof(TexturedQuad.Vertex), 
                Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.Position))
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
                sizeof(TexturedQuad.Vertex), 
                Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords))
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
                sizeof(Glyph), 
                Offset<Glyph>(nameof(Glyph.ScreenRect))
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
                sizeof(Glyph), 
                Offset<Glyph>(nameof(Glyph.TextureRect))
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
                sizeof(Glyph), 
                Offset<Glyph>(nameof(Glyph.Color))
            );
            glEnableVertexAttribArray(colorRectAttribLocation);
            glVertexAttribDivisor(colorRectAttribLocation, 1);
            AssertNoGlError();
        }

        public IText CreateText(Rect screenRect, TextStyle style, string text)
        {
            var position = CalculatePosition(screenRect, style, text);
            var cursor = position;
            var color = style.Color;
            var glyphs = new List<IGlyph>();
            foreach (var c in text)
            {
                if (c == '\n')
                {
                    cursor.X = position.X;
                    cursor.Y -= m_LineHeight;
                    continue;
                }
                
                if (!TryGetGlyph(c, out var fontChar))
                    continue;
                
                var xPos = cursor.X + fontChar.XOffset;
            
                var offsetFromTop = fontChar.YOffset - (m_Base - fontChar.Height);
                var yPos = cursor.Y - offsetFromTop;
            
                var uOffset = fontChar.X / m_ScaleW;
                var vOffset = fontChar.Y / m_ScaleH;
                var uScale = fontChar.Width / m_ScaleW;
                var vScale = fontChar.Height / m_ScaleH;

                var glyph = new GlyphImpl
                {
                    ScreenRect = new Rect(xPos, yPos, fontChar.Width, fontChar.Height),
                    TextureRect = new Rect(uOffset, vOffset, uScale, vScale),
                    Color = color
                };
                
                Register(glyph);
                
                //Console.WriteLine($"{c}: ({uOffset}, {vOffset})\t({uScale}, {vScale})");
                cursor.X += fontChar.XAdvance;
            }

            var textImpl = new TextImpl(this, glyphs);
            return textImpl;
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
        
        private bool TryGetGlyph(char c, out FontChar glyph)
        {
            var id = (int)c;
            return m_IdToGlyphTable.TryGetValue(id, out glyph);
        }
    }

    class TextImpl : IText
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
        
        public TextStyle TextStyle { get; set; }

        private readonly string m_Text;
        private readonly List<IGlyph> m_Glyphs;
        private readonly BitmapFontTextRenderingSystem m_TextRenderingSystem;

        public TextImpl(BitmapFontTextRenderingSystem renderingSystem, List<IGlyph> glyphs)
        {
            m_TextRenderingSystem = renderingSystem;
            m_Glyphs = glyphs;
        }

        private void UpdateGlyphs()
        {
            var cursor = m_TextRenderingSystem.CalculatePosition(ScreenRect, TextStyle, m_Text);
            var chars = m_Text.AsSpan();
            
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }

    class GlyphImpl : IGlyph
    {
        public Rect ScreenRect { get; set; }
        public Rect TextureRect { get; set; }
        public Color Color { get; set; }
        
        public event Action<IGlyph>? BecameDirty;
        public void Update(ref Glyph glyph)
        {
            glyph.ScreenRect = ScreenRect;
            glyph.TextureRect = TextureRect;
            glyph.Color = Color;
        }
    }

    abstract class RenderingSystem<T>
    {
        protected readonly HashSet<T> m_ItemsToRegister = new();
        protected readonly HashSet<T> m_ItemsToUnregister = new();
        protected readonly SortedSet<int> m_DirtyItems = new();
        protected readonly SortedSet<int> m_IdsToFill = new();
        protected readonly Dictionary<T, int> m_ItemToIndexTable = new();
        protected readonly Dictionary<int, T> m_IndexToItemTable = new();
        
        protected int m_DirtyItemCount;
        protected int m_ItemCount;
        
        public void Register(T item)
        {
            m_ItemsToRegister.Add(item);
            m_ItemsToUnregister.Remove(item);
        }

        public void Unregister(T item)
        {
            m_ItemsToUnregister.Add(item);
            m_ItemsToRegister.Remove(item);
        }
        
        protected void Item_OnBecameDirty(T item)
        {
            var id = m_ItemToIndexTable[item];
            m_DirtyItems.Add(id);
        }
    }
    
    sealed unsafe class PanelRenderingSystem : RenderingSystem<IPanel>, IPanelRenderingSystem
    {
        private const uint MaxPanelCount = 20000;
        
        private readonly IWindow m_Window;
        
        private uint m_Vao;
        private uint m_AttributesBuffer;
        private uint m_InstancesBuffer;
        private uint m_ShaderProgram;
        private int m_ProjectionMatrixUniformLocation;
        private Matrix4x4 m_ProjectionMatrix;

        public PanelRenderingSystem(IWindow window)
        {
            m_Window = window;
        }

        public void Load()
        {
            uint vao;
            glGenVertexArrays(1, &vao);
            AssertNoGlError();
            m_Vao = vao;

            Span<uint> buffers = stackalloc uint[2];
            fixed (uint* ptr = &buffers[0])
                glGenBuffers(buffers.Length, ptr);
            AssertNoGlError();

            m_AttributesBuffer = buffers[0];
            m_InstancesBuffer = buffers[1];
        
            glBindVertexArray(m_Vao);
            AssertNoGlError();
        
            SetupAttributesBuffer();
            SetupInstancesBuffer();
        
            m_ShaderProgram = new ShaderProgramBuilder()
                .WithVertexShader("Assets/uirect.vert.glsl")
                .WithFragmentShader("Assets/uirect.frag.glsl")
                .Build();

            var bytes = Encoding.ASCII.GetBytes("projection_matrix");
            fixed(byte* ptr = &bytes[0])
                m_ProjectionMatrixUniformLocation = glGetUniformLocation(m_ShaderProgram, ptr);
            AssertNoGlError();

            var screenWidth = m_Window.ScreenWidth;
            var screenHeight = m_Window.ScreenHeight;
            m_ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0f, screenWidth, 0f, screenHeight, 0.1f, 100f);
        }

        public void Unload()
        {
            glBindVertexArray(0);
            fixed(uint* ptr = &m_Vao)
                glDeleteVertexArrays(1, ptr);
            AssertNoGlError();
            m_Vao = 0;
            
            glBindBuffer(GL_ARRAY_BUFFER, 0);
            Span<uint> buffers = stackalloc uint[]
            {
                m_AttributesBuffer,
                m_InstancesBuffer
            };
            fixed (uint* ptr = &buffers[0])
                glDeleteBuffers(buffers.Length, ptr);
            AssertNoGlError();
            m_AttributesBuffer = 0;
            m_InstancesBuffer = 0;
            
            glUseProgram(0);
            glDeleteProgram(m_ShaderProgram);
            AssertNoGlError();
            m_ShaderProgram = 0;
        }

        public void Update()
        {
            //Console.WriteLine($"Unregistering {m_PanelsToUnregister.Count} panels");
            foreach (var panel in m_ItemsToUnregister)
            {
                panel.BecameDirty -= Item_OnBecameDirty;
                var id = m_ItemToIndexTable[panel];
                m_IdsToFill.Add(id);
                m_IndexToItemTable.Remove(id);
                m_ItemToIndexTable.Remove(panel);
            }
            m_ItemsToUnregister.Clear();
            
            //Console.WriteLine($"Registering {m_PanelsToRegister.Count} panels");
            foreach (var panel in m_ItemsToRegister)
            {
                panel.BecameDirty += Item_OnBecameDirty;
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

                m_ItemToIndexTable[panel] = id;
                m_IndexToItemTable[id] = panel;
                
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

            var maxDirtyPanelCount = maxIndex + 1;

            m_DirtyItemCount = 0;
            if (m_DirtyItems.Count > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
                AssertNoGlError();
                
                var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<OpenGLSandbox.Panel>(maxDirtyPanelCount), GL_MAP_WRITE_BIT);
                AssertNoGlError();
                
                var buffer = new Span<OpenGLSandbox.Panel>(bufferPtr, maxDirtyPanelCount);
            
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
                AssertNoGlError();
            }
            
            //Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
            
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();

            fixed (float* ptr = &m_ProjectionMatrix.M11)
                glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
            AssertNoGlError();
            
            glBindVertexArray(m_Vao);
            AssertNoGlError();
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_ItemCount);
            AssertNoGlError();
        }
        
        private void SetupAttributesBuffer()
        {
            glBindBuffer(GL_ARRAY_BUFFER, m_AttributesBuffer);
            AssertNoGlError();

            var texturedQuad = new TexturedQuad();
            glBufferData(GL_ARRAY_BUFFER, SizeOf<TexturedQuad>(), &texturedQuad, GL_STATIC_DRAW);
            AssertNoGlError();
            
            uint positionAttribIndex = 0;
            glVertexAttribPointer(
                positionAttribIndex, 
                2, 
                GL_FLOAT, 
                false, 
                sizeof(TexturedQuad.Vertex), 
                Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.Position))
            );
            glEnableVertexAttribArray(positionAttribIndex);

            uint normalAttribIndex = 1;
            glVertexAttribPointer(
                normalAttribIndex, 
                2, 
                GL_FLOAT,
                false, 
                sizeof(TexturedQuad.Vertex),
                Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords))
            );
            glEnableVertexAttribArray(normalAttribIndex);
        }
        
        private void SetupInstancesBuffer()
        {
            glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
            glBufferData(GL_ARRAY_BUFFER, SizeOf<OpenGLSandbox.Panel>(MaxPanelCount), (void*)0, GL_STREAM_DRAW);
            
            uint colorAttribIndex = 2;
            glVertexAttribPointer(
                colorAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(OpenGLSandbox.Panel), 
                Offset<OpenGLSandbox.Panel>(nameof(OpenGLSandbox.Panel.Color))
            );
            glEnableVertexAttribArray(colorAttribIndex);
            glVertexAttribDivisor(colorAttribIndex, 1);

            uint borderRadiusAttribIndex = 3;
            glVertexAttribPointer(
                borderRadiusAttribIndex, 
                4, GL_FLOAT,
                false, 
                sizeof(OpenGLSandbox.Panel), 
                Offset<OpenGLSandbox.Panel>(nameof(OpenGLSandbox.Panel.BorderRadius))
            );
            glEnableVertexAttribArray(borderRadiusAttribIndex);
            glVertexAttribDivisor(borderRadiusAttribIndex, 1);

            uint rectAttribIndex = 4;
            glVertexAttribPointer(
                rectAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(OpenGLSandbox.Panel), 
                Offset<OpenGLSandbox.Panel>(nameof(OpenGLSandbox.Panel.ScreenRect))
            );
            glEnableVertexAttribArray(rectAttribIndex);
            glVertexAttribDivisor(rectAttribIndex, 1);

            uint borderColorAttribIndex = 5;
            glVertexAttribPointer(
                borderColorAttribIndex, 
                4, GL_FLOAT, 
                false, 
                sizeof(OpenGLSandbox.Panel),
                Offset<OpenGLSandbox.Panel>(nameof(OpenGLSandbox.Panel.BorderColor))
            );
            glEnableVertexAttribArray(borderColorAttribIndex);
            glVertexAttribDivisor(borderColorAttribIndex, 1);
            
            uint borderSizeAttribIndex = 6;
            glVertexAttribPointer(
                borderSizeAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(OpenGLSandbox.Panel),
                Offset<OpenGLSandbox.Panel>(nameof(OpenGLSandbox.Panel.BorderSize))
            );
            glEnableVertexAttribArray(borderSizeAttribIndex);
            glVertexAttribDivisor(borderSizeAttribIndex, 1);
        }
    }
}