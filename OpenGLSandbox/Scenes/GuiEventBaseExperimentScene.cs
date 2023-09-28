using System.Numerics;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    public void Load()
    {
        
    }

    public void Render()
    {
        
    }

    public void Unload()
    {
        
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

    class PanelRenderer : IPanelRenderer
    {
        private readonly Panel[] m_Buffer = new Panel[10000];
        private readonly Dictionary<IPanel, int> m_PanelToIdTable = new();
        private readonly Dictionary<int, IPanel> m_IdToPanelTable = new();

        private readonly HashSet<IPanel> m_PanelsToRegister = new();
        private readonly HashSet<IPanel> m_PanelsToUnregister = new();
        private readonly HashSet<IPanel> m_DirtyPanels = new();
        private readonly SortedSet<int> m_IdsToFill = new();

        private int m_DirtyCount;
        private int m_PanelCount;
        
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
            foreach (var panel in m_PanelsToUnregister)
            {
                panel.BecameDirty -= Panel_OnBecameDirty;
                var id = m_PanelToIdTable[panel];
                m_IdsToFill.Add(id);
                m_IdToPanelTable.Remove(id);
                m_PanelToIdTable.Remove(panel);
            }
            m_PanelsToUnregister.Clear();
            
            foreach (var panel in m_PanelsToRegister)
            {
                panel.BecameDirty += Panel_OnBecameDirty;
                int id;
                if (m_IdsToFill.Count > 0)
                {
                    id = m_IdsToFill.Min;
                    m_IdsToFill.Remove(id);
                }
                else
                {
                    id = m_PanelCount;
                    m_PanelCount++;
                }

                m_PanelToIdTable[panel] = id;
                m_IdToPanelTable[id] = panel;
                
                m_DirtyPanels.Add(panel);
            }
            m_PanelsToRegister.Clear();
            
            foreach (var idToFill in m_IdsToFill.Reverse())
            {
                var lastPanelId = m_PanelCount - 1;
                if (idToFill != lastPanelId)
                {
                    var lastPanel = m_IdToPanelTable[lastPanelId];

                    m_IdToPanelTable.Remove(lastPanelId);
                    m_IdToPanelTable[idToFill] = lastPanel;
                    m_PanelToIdTable[lastPanel] = idToFill;

                    m_DirtyPanels.Add(lastPanel);
                }
                
                m_PanelCount--;
            }
            m_IdsToFill.Clear();

            foreach (var panel in m_DirtyPanels)
            {
                var id = m_PanelToIdTable[panel];
                if (id > m_DirtyCount)
                {
                    Swap(id, panel, m_DirtyCount);
                }

                m_DirtyCount++;
                panel.Update(ref m_Buffer[id]);
            }
            m_DirtyPanels.Clear();
        }

        private void Swap(int srcIndex, IPanel srcPanel, int dstIndex)
        {
            var dstPanel = m_IdToPanelTable[dstIndex];
            
            var dstPanelData = m_Buffer[dstIndex];
            m_Buffer[srcIndex] = dstPanelData;
            
            m_IdToPanelTable[srcIndex] = dstPanel;
            m_PanelToIdTable[dstPanel] = srcIndex;

            m_IdToPanelTable[dstIndex] = srcPanel;
            m_PanelToIdTable[srcPanel] = dstIndex;
        }

        private void Panel_OnBecameDirty(IPanel panel)
        {
            m_DirtyPanels.Add(panel);
        }
    }
}