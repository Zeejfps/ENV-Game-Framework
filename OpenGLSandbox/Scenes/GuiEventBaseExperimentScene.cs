using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    private readonly PanelRenderingSystem m_PanelRenderingSystem;
    private readonly BitmapFontTextRenderingSystem m_BitmapFontTextRenderingSystem;
    private TextButton[] m_TextButtons;
    
    public GuiEventBaseExperimentScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderingSystem = new PanelRenderingSystem(window);
        m_BitmapFontTextRenderingSystem = new BitmapFontTextRenderingSystem(window, "Assets/bitmapfonts/Segoe UI.fnt");
    }
    
    public void Load()
    {
        var w = 4;
        var h = 10;
        var padding = 2f;
        var buttonWidth = m_Window.ScreenWidth / (float)w;
        var buttonHeight = m_Window.ScreenHeight / (float)h;
        var buttonBorderColor = Color.FromHex(0xe8e2ea, 1f);
        
        m_TextButtons = new TextButton[w * h];
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                m_TextButtons[i * w + j] = new TextButton(m_PanelRenderingSystem, m_BitmapFontTextRenderingSystem)
                {
                    ScreenRect = new Rect(j*buttonWidth, i*buttonHeight, buttonWidth, buttonHeight),
                    BorderColor = buttonBorderColor,
                    BorderRadius = new Vector4(6f, 6f, 6f, 6),
                    BorderSize = BorderSize.All(1f),
                    Text = $"{i * w + j}"
                };
            }
        }
        
        var bg = Color.FromHex(0xf7f0f9, 1f);
        glClearColor(bg.R, bg.G, bg.B, bg.A);
        
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
            textButton.IsPressed = textButton.IsHovered && mouse.IsButtonPressed(MouseButton.Left);
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

    sealed class TextButton
    {
        private string m_Text;
        public string Text
        {
            get => m_Text;
            set => SetField(ref m_Text, value);
        }
        
        public Color BorderColor { get; set; }
        public BorderSize BorderSize { get; set; }
        public Vector4 BorderRadius { get; set; }
        public Rect ScreenRect { get; set; }
        
        private bool m_IsHovered;
        public bool IsHovered
        {
            get => m_IsHovered;
            set => SetField(ref m_IsHovered, value);
        }

        private bool m_IsPressed;
        public bool IsPressed
        {
            get => m_IsPressed;
            set => SetField(ref m_IsPressed, value);
        }
        
        private readonly IPanelRenderingSystem m_PanelRenderingSystem;
        private readonly ITextRenderingSystem m_TextRenderingSystem;

        private IRenderedText? m_RenderedText;
        private IRenderedPanel? m_RenderedPanel;
        private bool m_IsVisible;
        
        private Color BackgroundNormalColor { get; } = Color.FromHex(0xffffff, 1f);
        private Color BackgroundPressedColor { get; } = Color.FromHex(0xfaf7fc, 1f);
        private Color BackgroundHoveredColor { get; } = Color.FromHex(0xfdfbfd, 1f);
        
        private Color TextNormalColor { get; } = Color.FromHex(0x1b1a1b, 1f);
        private Color TextPressedColor { get; } = Color.FromHex(0x5f5e60, 1f);
        
        public TextButton(IPanelRenderingSystem panelRenderer, ITextRenderingSystem textRenderingSystem)
        {
            m_PanelRenderingSystem = panelRenderer;
            m_TextRenderingSystem = textRenderingSystem;
        }

        public void OnBecameVisible()
        {
            m_RenderedPanel = m_PanelRenderingSystem.Create(ScreenRect, new PanelStyle
            {
                BorderColor = BorderColor,
                BorderRadius = BorderRadius,
                BorderSize = BorderSize,
                BackgroundColor = BackgroundNormalColor
            });
            
            m_RenderedText = m_TextRenderingSystem.Create(ScreenRect, new TextStyle
            {
                Color = TextNormalColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }, Text);

            m_IsVisible = true;
        }

        public void OnBecameHidden()
        {
        }
        
        private void SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            
            if (m_IsVisible)
                UpdateView();
        }

        private void UpdateView()
        {
            m_RenderedPanel.ScreenRect = ScreenRect;
            m_RenderedText.ScreenRect = ScreenRect;

            var backgroundColor = BackgroundNormalColor;
            var textColor = TextNormalColor;
            if (IsPressed)
            {
                backgroundColor = BackgroundPressedColor;
                textColor = TextPressedColor;
            }
            else if (IsHovered)
            {
                backgroundColor = BackgroundHoveredColor;
            }
            
            m_RenderedPanel.Style = new PanelStyle
            {
                BackgroundColor = backgroundColor,
                BorderColor = BorderColor,
                BorderRadius = BorderRadius,
                BorderSize = BorderSize
            };
            
            m_RenderedText.Style = new TextStyle
            {
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                Color = textColor
            };
        }
    }

    public struct PanelStyle
    {
        public Color BackgroundColor;
        public Color BorderColor;
        public BorderSize BorderSize;
        public Vector4 BorderRadius;
    }

    public interface IRenderedPanel
    {
        Rect ScreenRect { get; set; }
        PanelStyle Style { get; set; }
    }

    public interface IPanelRenderingSystem
    {
        IRenderedPanel Create(Rect screenPosition, PanelStyle style);
    }

    // NOTE(Zee): The probably doesn't need to exist?

    public abstract class RenderingSystem<T>
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
    
    sealed unsafe class PanelRenderingSystem : RenderingSystem<RenderedPanelImpl>, IPanelRenderingSystem
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
                
                var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<Panel>(maxDirtyPanelCount), GL_MAP_WRITE_BIT);
                AssertNoGlError();
                
                var buffer = new Span<Panel>(bufferPtr, maxDirtyPanelCount);
            
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
            glBufferData(GL_ARRAY_BUFFER, SizeOf<Panel>(MaxPanelCount), (void*)0, GL_DYNAMIC_DRAW);
            
            uint colorAttribIndex = 2;
            glVertexAttribPointer(
                colorAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(Panel), 
                Offset<Panel>(nameof(Panel.BackgroundColor))
            );
            glEnableVertexAttribArray(colorAttribIndex);
            glVertexAttribDivisor(colorAttribIndex, 1);

            uint borderRadiusAttribIndex = 3;
            glVertexAttribPointer(
                borderRadiusAttribIndex, 
                4, GL_FLOAT,
                false, 
                sizeof(Panel), 
                Offset<Panel>(nameof(Panel.BorderRadius))
            );
            glEnableVertexAttribArray(borderRadiusAttribIndex);
            glVertexAttribDivisor(borderRadiusAttribIndex, 1);

            uint rectAttribIndex = 4;
            glVertexAttribPointer(
                rectAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(Panel), 
                Offset<Panel>(nameof(Panel.ScreenRect))
            );
            glEnableVertexAttribArray(rectAttribIndex);
            glVertexAttribDivisor(rectAttribIndex, 1);

            uint borderColorAttribIndex = 5;
            glVertexAttribPointer(
                borderColorAttribIndex, 
                4, GL_FLOAT, 
                false, 
                sizeof(Panel),
                Offset<Panel>(nameof(Panel.BorderColor))
            );
            glEnableVertexAttribArray(borderColorAttribIndex);
            glVertexAttribDivisor(borderColorAttribIndex, 1);
            
            uint borderSizeAttribIndex = 6;
            glVertexAttribPointer(
                borderSizeAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(Panel),
                Offset<Panel>(nameof(Panel.BorderSize))
            );
            glEnableVertexAttribArray(borderSizeAttribIndex);
            glVertexAttribDivisor(borderSizeAttribIndex, 1);
        }

        public IRenderedPanel Create(Rect screenRect, PanelStyle style)
        {
            var p = new RenderedPanelImpl
            {
                ScreenRect = screenRect,
                Style = style
            };
            
            Register(p);
            return p;
        }
    }

    sealed class RenderedPanelImpl : IRenderedPanel
    {
        private Rect m_ScreenRect;

        public Rect ScreenRect
        {
            get => m_ScreenRect;
            set
            {
                m_ScreenRect = value;
                BecameDirty?.Invoke(this);
            }
        }

        private PanelStyle m_Style;
        public PanelStyle Style
        {
            get => m_Style;
            set
            {
                m_Style = value;
                BecameDirty?.Invoke(this);
            }
        }

        public event Action<RenderedPanelImpl>? BecameDirty;
        
        public void Update(ref Panel panel)
        {
            var style = Style;
            panel.ScreenRect = ScreenRect;
            panel.BackgroundColor = style.BackgroundColor;
            panel.BorderRadius = style.BorderRadius;
            panel.BorderSize = style.BorderSize;
            panel.BorderColor = style.BorderColor;
        }
    }
}

public interface IRenderedText : IDisposable
{
    Rect ScreenRect { get; set; }
    TextStyle Style { get; set; }
}

public interface ITextRenderingSystem
{
    IRenderedText Create(Rect screenPosition, TextStyle style, string value);
}

public interface IRenderedGlyph
{
}

public class RenderedGlyphImpl : IRenderedGlyph
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

    public event Action<RenderedGlyphImpl>? BecameDirty;
    public void Update(ref Glyph glyph)
    {
        glyph.ScreenRect = ScreenRect;
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