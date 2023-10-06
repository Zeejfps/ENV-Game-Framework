using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using static GL46;

namespace OpenGLSandbox;

public sealed class GuiEventBaseExperimentScene : IScene
{
    private readonly IWindow m_Window;
    private readonly IInputSystem m_InputSystem;
    private readonly PanelRenderer m_PanelRenderer;
    private readonly BitmapFontTextRenderer m_BitmapFontTextRenderer;
    private TextButton[]? m_TextButtons;
    
    public GuiEventBaseExperimentScene(IWindow window, IInputSystem inputSystem)
    {
        m_Window = window;
        m_InputSystem = inputSystem;
        m_PanelRenderer = new PanelRenderer(window);
        m_BitmapFontTextRenderer = new BitmapFontTextRenderer(window);
    }
    
    public void Load()
    {
        m_Window.Title = "Calculator";
        
        var w = 4;
        var h = 9;
        var padding = 4f;
        var screenWidth = m_Window.ScreenWidth - padding * 2f - padding * (w-1);
        var screenHeight = m_Window.ScreenHeight - padding * 2f - padding * (h-1);
        var buttonWidth = screenWidth / w;
        var buttonHeight = screenHeight / h;
        var buttonBorderColor = Color.FromHex(0xe8e2ea, 1f);
        
        m_TextButtons = new TextButton[w * h];
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                m_TextButtons[i * w + j] = new TextButton(m_PanelRenderer, m_BitmapFontTextRenderer)
                {
                    ScreenRect = new Rect(j*(buttonWidth+padding) + padding, i*(buttonHeight+padding)+padding, buttonWidth, buttonHeight),
                    BorderColor = buttonBorderColor,
                    BorderRadius = new Vector4(6f, 6f, 6f, 6),
                    BorderSize = BorderSize.All(1f),
                    Text = $"{i * w + j}"
                };
            }
        }
        
        var bg = Color.FromHex(0xf7f0f9, 1f);
        glClearColor(bg.R, bg.G, bg.B, bg.A);
        
        m_PanelRenderer.Load();
        m_BitmapFontTextRenderer.Load(new []
        {
            new BmpFontFile
            {
                FontName = "Segoe UI",
                PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
            }
        });
        foreach (var textButton in m_TextButtons)
        {
            textButton.IsVisible = true;
        }
    }

    public void Update()
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
        m_PanelRenderer.Update();
        m_BitmapFontTextRenderer.Update();
    }

    public void Unload()
    {
        m_TextButtons = null;
        m_PanelRenderer.Unload();
        m_BitmapFontTextRenderer.Unload();
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

        private bool m_IsVisible;
        public bool IsVisible
        {
            get => m_IsVisible;
            set
            {
                if (m_IsVisible == value)
                    return;
                m_IsVisible = value;
                if (m_IsVisible)
                    OnBecameVisible();
                else
                    OnBecameHidden();
            }
        }
        
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
            set
            {
                if (m_IsPressed == value)
                    return;

                m_IsPressed = value;
                if (m_IsPressed)
                    OnPressed();
                else
                    OnReleased();
            }
        }

        private void OnPressed()
        {
            if (m_IsVisible)
                UpdateView();
        }

        private void OnReleased()
        {
            IsVisible = !IsVisible;
        }

        private readonly IPanelRenderer m_PanelRenderer;
        private readonly ITextRenderer m_TextRenderer;

        private IRenderedText? m_RenderedText;
        private IRenderedPanel? m_RenderedPanel;
        
        private Color BackgroundNormalColor { get; set; } = Color.FromHex(0xffffff, 1f);
        private Color BackgroundPressedColor { get; set; } = Color.FromHex(0xfaf7fc, 1f);
        private Color BackgroundHoveredColor { get; set; } = Color.FromHex(0xfdfbfd, 1f);
        private Color TextNormalColor { get; set; } = Color.FromHex(0x1b1a1b, 1f);
        private Color TextPressedColor { get; set; } = Color.FromHex(0x5f5e60, 1f);
        
        public TextButton(IPanelRenderer panelRenderer, ITextRenderer textRenderer)
        {
            m_PanelRenderer = panelRenderer;
            m_TextRenderer = textRenderer;
        }

        private void OnBecameVisible()
        {
            if (Text == "3")
            {
                BackgroundNormalColor = Color.FromHex(0x0067c0, 1f);
                BackgroundHoveredColor = Color.FromHex(0x1974c5, 1f);
                BackgroundPressedColor = Color.FromHex(0x3182cb, 1f);
                TextNormalColor = Color.FromHex(0xdae9f6, 1f);
                TextPressedColor = Color.FromHex(0xadcdea, 1f);
            }
            
            m_RenderedPanel = m_PanelRenderer.Render(ScreenRect, new PanelStyle
            {
                BorderColor = BorderColor,
                BorderRadius = BorderRadius,
                BorderSize = BorderSize,
                BackgroundColor = BackgroundNormalColor
            });
            
            m_RenderedText = m_TextRenderer.Render(Text, ScreenRect, new TextStyle
            {
                FontName = "Segoe UI",
                Color = TextNormalColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        private void OnBecameHidden()
        {
            m_RenderedPanel?.Dispose();
            m_RenderedText?.Dispose();

            m_RenderedPanel = null;
            m_RenderedText = null;
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
}

public interface IRenderedGlyph
{
}