using System.Diagnostics;
using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using static GL46;

namespace OpenGLSandbox;

public sealed class CalculatorScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    
    private readonly PanelRenderer m_PanelRenderer;
    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    private TextButton? m_TextButton;

    private readonly List<Widget> m_Widgets = new();

    private readonly PanelStyle m_ButtonStyleNormal = new()
    {
        BackgroundColor = Color.FromHex(0x3e3940, 1f),
        BorderRadius = new Vector4(7f, 7f, 7f, 7f),
        BorderColor = Color.FromHex(0x322e35, 1f),
        BorderSize = BorderSize.All(1f)
    };
    
    private readonly PanelStyle m_ButtonStylePressed = new()
    {
        BackgroundColor = Color.FromHex(0x2a262e, 1f),
        BorderRadius = new Vector4(7f, 7f, 7f, 7f),
        BorderColor = Color.FromHex(0x332e35, 1f),
        BorderSize = BorderSize.All(1f)
    };

    private readonly TextStyle m_ButtonTextStyleNormal = new()
    {
        Color = Color.FromHex(0xf7f7f7, 1f),
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Center
    };
    
    private readonly TextStyle m_ButtonTextStylePressed = new()
    {
        Color = Color.FromHex(0xc8c7c9, 1f),
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Center
    };

    public CalculatorScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
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
            ScreenRect = new Rect(10f, 10f, 200f, 60f),
            Text = "+/-",
            PanelStyleNormal = m_ButtonStyleNormal,
            PanelStylePressed = m_ButtonStylePressed,
            TextStyleNormal = m_ButtonTextStyleNormal,
            TextStylePressed = m_ButtonTextStylePressed,
            IsVisible = true
        };
        m_Widgets.Add(m_TextButton);

        var bgColor = Color.FromHex(0x221e26, 1f);
        glClearColor(bgColor.R, bgColor.G, bgColor.B, bgColor.A);
    }

    public void Update()
    {
        var mouse = m_InputSystem.Mouse;
        var mouseScreenPosition = new Vector2(mouse.ScreenX, m_Window.ScreenHeight - mouse.ScreenY);
        foreach (var widget in m_Widgets)
        {
            widget.IsHovered = widget.ScreenRect.Contains(mouseScreenPosition);
            widget.IsPressed = widget.IsHovered && mouse.IsButtonPressed(MouseButton.Left);
            widget.Update();
        }
        
        glClear(GL_COLOR_BUFFER_BIT);
        m_PanelRenderer.Update();
        m_TextRenderer.Update();
    }

    public void Unload()
    {
        m_Widgets.Clear();
        m_PanelRenderer.Unload();
        m_TextRenderer.Unload();
    }

    abstract class Widget
    {
        private Rect m_ScreenRect;
        public Rect ScreenRect
        {
            get => m_ScreenRect;
            set => SetField(ref m_ScreenRect, value);
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
            SetDirty();
        }
        
        protected abstract void OnBecameVisible();
        protected abstract void OnBecameHidden();

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

        private TextStyle m_TextStyleNormal;
        public TextStyle TextStyleNormal
        {
            get => m_TextStyleNormal;
            set => SetField(ref m_TextStyleNormal, value);
        }

        private TextStyle m_TextStylePressedPressed;
        public TextStyle TextStylePressed
        {
            get => m_TextStylePressedPressed;
            set => SetField(ref m_TextStylePressedPressed, value);
        }

        private PanelStyle m_PanelStyleNormal;
        public PanelStyle PanelStyleNormal
        {
            get => m_PanelStyleNormal;
            set => SetField(ref m_PanelStyleNormal, value);
        }
        
        private PanelStyle m_PanelStylePressed;
        public PanelStyle PanelStylePressed
        {
            get => m_PanelStylePressed;
            set => SetField(ref m_PanelStylePressed, value);
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
            m_RenderedPanel = m_PanelRenderer.Render(ScreenRect, PanelStyleNormal);
            m_RenderedText = m_TextRenderer.Render(Text, ScreenRect, TextStyleNormal);
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

            var panelStyle = PanelStyleNormal;
            var textStyle = TextStyleNormal;
            if (IsPressed)
            {
                textStyle = TextStylePressed;
                panelStyle = PanelStylePressed;
            }
            else if (IsHovered)
            {
            }
            
            m_RenderedPanel.ScreenRect = ScreenRect;
            m_RenderedPanel.Style = panelStyle;
            
            m_RenderedText.ScreenRect = ScreenRect;
            m_RenderedText.Style = textStyle;
        }
    }
}

