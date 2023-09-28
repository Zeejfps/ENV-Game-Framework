using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    private readonly PanelRenderingSystem m_PanelRenderingSystem;
    private readonly TextButton[] m_TextButtons;
    
    public GuiEventBaseExperimentScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderingSystem = new PanelRenderingSystem(window);

        var w = 10;
        var h = 10;
        var buttonSize = window.ScreenWidth / (float)w;
        var buttonBorderColor = Color.FromHex(0xff00ff, 1f);
        
        m_TextButtons = new TextButton[w * h];
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                m_TextButtons[i * w + j] = new TextButton(m_PanelRenderingSystem)
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
    }

    public void Unload()
    {
        m_PanelRenderingSystem.Unload();
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

        private readonly IPanelRenderingSystem m_PanelRenderer;

        public TextButton(IPanelRenderingSystem panelRenderer)
        {
            m_PanelRenderer = panelRenderer;
        }

        public void OnBecameVisible()
        {
            m_PanelRenderer.Register(this);
        }

        public void OnBecameHidden()
        {
            m_PanelRenderer.Unregister(this);
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

    interface IPanel
    {
        event Action<IPanel> BecameDirty;
        void Update(ref Panel panel);
    }

    interface IPanelRenderingSystem
    {
        void Register(IPanel panel);
        void Unregister(IPanel panel);
    }

    interface IText
    {
        event Action BecameDirty;
        void Update(ref Glyph glyph);
    }
    
    interface ITextRenderingSystem
    {
        void Register(IText panel);
        void Unregister(IText panel);
    }

    sealed unsafe class TextRenderingSystem : RenderingSystem<IText>, ITextRenderingSystem
    {
        private const uint MaxGlyphCount = 20000;

        private readonly IWindow m_Window;
        
        private uint m_VertexArray;
        private uint m_AttributesBuffer;
        private uint m_InstancesBuffer;
        private uint m_ShaderProgram;
        private uint m_Texture;
        private int m_ProjectionMatrixUniformLocation;
        private Matrix4x4 m_ProjectionMatrix;
        private int m_GlyphCount;

        public TextRenderingSystem(IWindow window)
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
            
            m_ShaderProgram = new ShaderProgramBuilder()
                .WithVertexShader("Assets/Shaders/bmpfont.vert.glsl")
                .WithFragmentShader("Assets/Shaders/bmpfont.frag.glsl")
                .Build();

            m_ProjectionMatrixUniformLocation = GetUniformLocation(m_ShaderProgram, "u_ProjectionMatrix");
            m_ProjectionMatrix= Matrix4x4.CreateOrthographicOffCenter(0f, m_Window.ScreenWidth, 0f, m_Window.ScreenHeight, 0.1f, 100f);
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
        }

        public void Update()
        {
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();
            
            fixed (float* ptr = &m_ProjectionMatrix.M11)
                glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
            AssertNoGlError();
            
            glBindVertexArray(m_VertexArray);
            AssertNoGlError();
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_GlyphCount);
            AssertNoGlError();
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
    }

    abstract class RenderingSystem<T>
    {
        protected readonly HashSet<T> m_ItemsToRegister = new();
        protected readonly HashSet<T> m_ItemsToUnregister = new();
        protected readonly SortedSet<int> m_DirtyItems = new();
        protected readonly SortedSet<int> m_IdsToFill = new();
        
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
    }
    
    sealed unsafe class PanelRenderingSystem : RenderingSystem<IPanel>, IPanelRenderingSystem
    {
        private const uint MaxPanelCount = 20000;
        
        private readonly Dictionary<IPanel, int> m_PanelToIdTable = new();
        private readonly Dictionary<int, IPanel> m_IdToPanelTable = new();
        
        private readonly IWindow m_Window;
        
        private int m_DirtyCount;
        private int m_PanelCount;
        
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
                panel.BecameDirty -= Panel_OnBecameDirty;
                var id = m_PanelToIdTable[panel];
                m_IdsToFill.Add(id);
                m_IdToPanelTable.Remove(id);
                m_PanelToIdTable.Remove(panel);
            }
            m_ItemsToUnregister.Clear();
            
            //Console.WriteLine($"Registering {m_PanelsToRegister.Count} panels");
            foreach (var panel in m_ItemsToRegister)
            {
                panel.BecameDirty += Panel_OnBecameDirty;
                int id;
                if (m_IdsToFill.Count > 0)
                {
                    id = m_IdsToFill.Min;
                    //Console.WriteLine($"Reusing an id that needs to be filled. Id: {id}");
                    m_IdsToFill.Remove(id);
                }
                else
                {
                    id = m_PanelCount;
                    //Console.WriteLine($"Assigned a new id. Id: {id}");
                    m_PanelCount++;
                }

                m_PanelToIdTable[panel] = id;
                m_IdToPanelTable[id] = panel;
                
                m_DirtyItems.Add(id);
            }
            m_ItemsToRegister.Clear();
            
            //Console.WriteLine($"Back filling {m_IdsToFill.Count} ids");
            foreach (var idToFill in m_IdsToFill.Reverse())
            {
                var lastPanelId = m_PanelCount - 1;
                if (idToFill != lastPanelId)
                {
                    //Console.WriteLine($"Moving last panel into an id we need to fill. Id: {idToFill}");
                    var lastPanel = m_IdToPanelTable[lastPanelId];

                    m_IdToPanelTable.Remove(lastPanelId);
                    m_IdToPanelTable[idToFill] = lastPanel;
                    m_PanelToIdTable[lastPanel] = idToFill;

                    m_DirtyItems.Add(idToFill);
                }
                
                m_PanelCount--;
            }
            m_IdsToFill.Clear();

            var maxIndex = m_DirtyItems.Max;
            //Console.WriteLine($"Max dirty panel index {maxIndex}");

            var maxDirtyPanelCount = maxIndex + 1;

            m_DirtyCount = 0;
            if (m_DirtyItems.Count > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
                AssertNoGlError();
                var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<Panel>(maxDirtyPanelCount), GL_MAP_WRITE_BIT);
                AssertNoGlError();
                var buffer = new Span<Panel>(bufferPtr, maxDirtyPanelCount);
            
                foreach (var panelId in m_DirtyItems)
                {
                    var srcPanel = m_IdToPanelTable[panelId];
                    var dstIndex = m_DirtyCount;

                    if (panelId > m_DirtyCount)
                    {
                        //Console.WriteLine($"Swaping {panelId} with {dstIndex}");
                        var srcIndex = panelId;

                        var dstPanel = m_IdToPanelTable[dstIndex];
            
                        var dstPanelData = buffer[dstIndex];
                        buffer[srcIndex] = dstPanelData;
            
                        m_IdToPanelTable[srcIndex] = dstPanel;
                        m_PanelToIdTable[dstPanel] = srcIndex;

                        m_IdToPanelTable[dstIndex] = srcPanel;
                        m_PanelToIdTable[srcPanel] = dstIndex;
                    }

                    srcPanel.Update(ref buffer[dstIndex]);
                    m_DirtyCount++;
                }
                m_DirtyItems.Clear();
                glUnmapBuffer(GL_ARRAY_BUFFER);
            }
            
            //Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
            
            glUseProgram(m_ShaderProgram);
            AssertNoGlError();

            fixed (float* ptr = &m_ProjectionMatrix.M11)
                glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
            AssertNoGlError();
            
            glBindVertexArray(m_Vao);
            AssertNoGlError();
            glDrawArraysInstanced(GL_TRIANGLES, 0, 6, m_PanelCount);
            AssertNoGlError();
        }

        private void Panel_OnBecameDirty(IPanel panel)
        {
            var id = m_PanelToIdTable[panel];
            m_DirtyItems.Add(id);
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
            glBufferData(GL_ARRAY_BUFFER, SizeOf<Panel>(MaxPanelCount), (void*)0, GL_STREAM_DRAW);
            
            uint colorAttribIndex = 2;
            glVertexAttribPointer(
                colorAttribIndex, 
                4, 
                GL_FLOAT, 
                false, 
                sizeof(Panel), 
                Offset<Panel>(nameof(Panel.Color))
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
    }
}