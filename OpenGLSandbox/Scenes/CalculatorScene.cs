using System.Diagnostics;
using EasyGameFramework.Api;
using static GL46;

namespace OpenGLSandbox;

public sealed class CalculatorScene : IScene
{
    private readonly IWindow m_Window;

    private readonly PanelRenderer m_PanelRenderer;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    private TextButton? m_TextButton;

    public CalculatorScene(IWindow window)
    {
        m_Window = window;
        m_PanelRenderer = new PanelRenderer(window);
        m_TextRenderer = new BitmapFontTextRenderer(window, "Assets/bitmapfonts/Segoe UI.fnt");
    }

    public void Load()
    {
        m_Window.SetScreenSize(400, 640);
        m_Window.Title = "Calculator";

        m_PanelRenderer.Load();
        m_TextRenderer.Load();
        
        m_TextButton = new TextButton(m_PanelRenderer, m_TextRenderer)
        {
            ScreenPosition = new Rect(0f, 0f, 200f, 60f),
            Text = "Hello World!",
            TextStyle = new TextStyle
            {
                Color = Color.FromHex(0xff00ff, 1f),
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.Center
            },
            IsVisible = true
        };
    }

    public void Update()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        
        m_TextButton.Update();
        
        m_PanelRenderer.Update();
        m_TextRenderer.Update();
    }

    public void Unload()
    {
        m_PanelRenderer.Unload();
        m_TextRenderer.Unload();
    }

    abstract class Widget
    {
        private Rect m_ScreenPosition;
        public Rect ScreenPosition
        {
            get => m_ScreenPosition;
            set
            {
                m_ScreenPosition = value;
                SetDirty();
            }
        }
        
        private bool m_IsVisible;
        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (m_IsVisible == value)
                    return;
                
                m_IsVisible = value;
                OnVisibilityStateChanged();
            }
        }

        protected virtual void OnVisibilityStateChanged()
        {
            if (m_IsVisible)
                OnBecameVisible();
            else
                OnBecameHidden();
        }
        
        protected abstract void OnBecameVisible();
        protected abstract void OnBecameHidden();

        private bool m_IsHovered;
        private bool m_IsPressed;
        private bool m_IsDirty;

        public void Update()
        {
            if (m_IsDirty && m_IsVisible)
                UpdateView();
            m_IsDirty = false;
        }

        protected bool SetField<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            SetDirty();
            return true;
        }
        
        protected void SetDirty()
        {
            m_IsDirty = true;
        }

        protected abstract void UpdateView();
    }
    
    sealed class TextButton : Widget
    {
        private string m_Text;
        public string Text
        {
            get => m_Text;
            set => SetField(ref m_Text, value);
        }

        private TextStyle m_TextStyle;
        public TextStyle TextStyle
        {
            get => m_TextStyle;
            set => SetField(ref m_TextStyle, value);
        }

        private PanelStyle m_PanelStyle;
        public PanelStyle PanelStyle
        {
            get => m_PanelStyle;
            set => SetField(ref m_PanelStyle, value);
        }
        
        private readonly ITextRenderer m_TextRenderer;
        private readonly IPanelRenderer m_PanelRenderer;
        
        private IRenderedPanel? m_RenderedPanel;
        private IRenderedText? m_RenderedText;

        public TextButton(IPanelRenderer panelRenderer, ITextRenderer textRenderer)
        {
            m_PanelRenderer = panelRenderer;
            m_TextRenderer = textRenderer;
        }

        protected override void OnBecameVisible()
        {
            m_RenderedPanel = m_PanelRenderer.Render(ScreenPosition, PanelStyle);
            m_RenderedText = m_TextRenderer.Render(Text, ScreenPosition, TextStyle);
        }

        protected override void OnBecameHidden()
        {
            m_RenderedPanel?.Dispose();
            m_RenderedPanel = null;
            
            m_RenderedText?.Dispose();
            m_RenderedText = null;
        }

        protected override void UpdateView()
        {
            Debug.Assert(m_RenderedPanel != null);
            Debug.Assert(m_RenderedText != null);

            m_RenderedPanel.ScreenRect = ScreenPosition;
            m_RenderedPanel.Style = PanelStyle;
            
            m_RenderedText.ScreenRect = ScreenPosition;
            m_RenderedText.Style = TextStyle;
        }
    }
}

