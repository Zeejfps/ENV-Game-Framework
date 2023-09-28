using System.Numerics;
using System.Text;
using EasyGameFramework.Api;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    private readonly PanelRenderer m_PanelRenderer;

    public GuiEventBaseExperimentScene(IWindow window)
    {
        m_PanelRenderer = new PanelRenderer(window);
    }
    
    public void Load()
    {
        m_PanelRenderer.Load();   
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        m_PanelRenderer.Update();
    }

    public void Unload()
    {
        m_PanelRenderer.Unload();
    }
    
    sealed class TextButton : IPanel
    {
        public event Action<IPanel>? BecameDirty;

        private bool m_IsHovered;
        private bool IsHovered
        {
            get => m_IsHovered;
            set => SetField(ref m_IsHovered, value);
        }

        private IPanelRenderer PanelRenderer { get; }
        private ITextRenderer TextRenderer { get; }

        public void OnBecameVisible()
        {
            PanelRenderer.Register(this);
        }

        public void OnBecameHidden()
        {
            PanelRenderer.Unregister(this);
        }


        public void Update(ref Panel panel)
        {
            
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

    interface IPanelRenderer
    {
        void Register(IPanel panel);
        void Unregister(IPanel panel);
    }

    interface IText
    {
        event Action BecameDirty;
        void Update(ref Panel panel);
    }
    
    interface ITextRenderer
    {
        void Register(IText panel);
        void Unregister(IText panel);
    }

    unsafe class PanelRenderer : IPanelRenderer
    {
        private const uint MaxPanelCount = 20000;
        
        private readonly Dictionary<IPanel, int> m_PanelToIdTable = new();
        private readonly Dictionary<int, IPanel> m_IdToPanelTable = new();

        private readonly HashSet<IPanel> m_PanelsToRegister = new();
        private readonly HashSet<IPanel> m_PanelsToUnregister = new();
        private readonly SortedSet<int> m_DirtyPanels = new();
        private readonly SortedSet<int> m_IdsToFill = new();

        private readonly IWindow m_Window;
        
        private int m_DirtyCount;
        private int m_PanelCount;
        
        private uint m_Vao;
        private uint m_AttributesBuffer;
        private uint m_InstancesBuffer;
        private uint m_ShaderProgram;
        private int m_ProjectionMatrixUniformLocation;
        private Matrix4x4 m_ProjectionMatrix;

        public PanelRenderer(IWindow window)
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
        
        public void Register(IPanel panel)
        {
            m_PanelsToRegister.Add(panel);
            m_PanelsToUnregister.Remove(panel);
        }

        public void Unregister(IPanel panel)
        {
            m_PanelsToUnregister.Add(panel);
            m_PanelsToRegister.Remove(panel);
        }

        public void Update()
        {
            Console.WriteLine($"Unregistering {m_PanelsToUnregister.Count} panels");
            foreach (var panel in m_PanelsToUnregister)
            {
                panel.BecameDirty -= Panel_OnBecameDirty;
                var id = m_PanelToIdTable[panel];
                m_IdsToFill.Add(id);
                m_IdToPanelTable.Remove(id);
                m_PanelToIdTable.Remove(panel);
            }
            m_PanelsToUnregister.Clear();
            
            Console.WriteLine($"Registering {m_PanelsToUnregister.Count} panels");
            foreach (var panel in m_PanelsToRegister)
            {
                panel.BecameDirty += Panel_OnBecameDirty;
                int id;
                if (m_IdsToFill.Count > 0)
                {
                    id = m_IdsToFill.Min;
                    Console.WriteLine($"Reusing an id that needs to be filled. Id: {id}");
                    m_IdsToFill.Remove(id);
                }
                else
                {
                    id = m_PanelCount;
                    Console.WriteLine($"Assigned a new id. Id: {id}");
                    m_PanelCount++;
                }

                m_PanelToIdTable[panel] = id;
                m_IdToPanelTable[id] = panel;
                
                m_DirtyPanels.Add(id);
            }
            m_PanelsToRegister.Clear();
            
            Console.WriteLine($"Back filling {m_IdsToFill.Count} ids");
            foreach (var idToFill in m_IdsToFill.Reverse())
            {
                var lastPanelId = m_PanelCount - 1;
                if (idToFill != lastPanelId)
                {
                    Console.WriteLine($"Moving last panel into an id we need to fill. Id: {idToFill}");
                    var lastPanel = m_IdToPanelTable[lastPanelId];

                    m_IdToPanelTable.Remove(lastPanelId);
                    m_IdToPanelTable[idToFill] = lastPanel;
                    m_PanelToIdTable[lastPanel] = idToFill;

                    m_DirtyPanels.Add(idToFill);
                }
                
                m_PanelCount--;
            }
            m_IdsToFill.Clear();

            var maxIndex = m_DirtyPanels.Max;
            Console.WriteLine($"Max dirty panel index {maxIndex}");

            m_DirtyCount = 0;
            if (m_DirtyPanels.Count > 0)
            {
                glBindBuffer(GL_ARRAY_BUFFER, m_InstancesBuffer);
                AssertNoGlError();
                var bufferPtr = glMapBufferRange(GL_ARRAY_BUFFER, IntPtr.Zero, SizeOf<Panel>(maxIndex), GL_MAP_WRITE_BIT);
                AssertNoGlError();
                var buffer = new Span<Panel>(bufferPtr, maxIndex);
            
                foreach (var panelId in m_DirtyPanels)
                {
                    var srcPanel = m_IdToPanelTable[panelId];
                    var dstIndex = m_DirtyCount;

                    if (panelId > m_DirtyCount)
                    {
                        Console.WriteLine($"Swaping {panelId} with {dstIndex}");
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
                m_DirtyPanels.Clear();
                glUnmapBuffer(GL_ARRAY_BUFFER);
            }
            
            Console.WriteLine($"Dirty Count: {m_DirtyCount}, Panel Count: {m_PanelCount}");
        }

        private void Panel_OnBecameDirty(IPanel panel)
        {
            var id = m_PanelToIdTable[panel];
            m_DirtyPanels.Add(id);
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