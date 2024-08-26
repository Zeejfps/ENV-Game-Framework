using System.Numerics;
using System.Text;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class TextField : StatefulWidget
{
    private StringBuilder m_StringBuilder = new();
    private readonly InputListenerController m_InputListenerController = new();

    private bool m_IsFocused;
    private bool IsFocused
    {
        get => m_IsFocused;
        set => SetField(ref m_IsFocused, value);
    }

    private IRenderedText? m_RenderedText;

    protected override void DisposeContent()
    {
        m_RenderedText?.Dispose();
        m_RenderedText = null;
        base.DisposeContent();
    }

    protected override IWidget Build(IBuildContext context)
    {
        var normalBackgroundColor = Color.FromHex(0x2D2D2D, 1f);
        var focusedBackgroundColor = Color.FromHex(0x1F1F1F, 1f);
        
        var normalBorderColor = Color.FromHex(0x9A9A9A, 1f);
        var focusedBorderColor = Color.FromHex(0xDB9EE5, 1f);

        var text = m_StringBuilder.ToString();
        var textRenderer = context.TextRenderer;

        var textFont = "Segoe UI";
        var textStyle = new TextStyle
        {
            Color = Color.FromHex(0xffffff, 1f),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        m_RenderedText = textRenderer.Render(text, textFont, ScreenRect, textStyle);
        
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            OnKeyPressed = OnKeyPressed,
            OnFocusGained = () => IsFocused = true,
            OnFocusLost = () => IsFocused = false,
            Child = new StackWidget
            {
                Children =
                {
                    new PanelWidget
                    {
                        Style = new PanelStyle
                        {
                            BackgroundColor = IsFocused ? focusedBackgroundColor : normalBackgroundColor,
                            BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                            BorderSize = BorderSize.FromTRBL(0f, 0f, 3f, 0f),
                            BorderColor = IsFocused ? focusedBorderColor : normalBorderColor,
                        }
                    },
                    new Caret
                    {
                        RenderedText = m_RenderedText,
                        GlyphIndex = text.Length - 1,
                    }
                }
            },
        };
    }

    private void OnKeyPressed(KeyboardKey key)
    {
        Console.WriteLine($"Key pressed: {key}");
        if (key == KeyboardKey.Backspace)
        {
            if (m_StringBuilder.Length == 0)
                return;
            
            m_StringBuilder.Remove(m_StringBuilder.Length - 1, 1);
        }
        else
        {
            m_StringBuilder.Append(key.ToString().ToLower());
        }
        
        SetDirty();
    }
}

public sealed class Caret : Widget
{
    
    public IRenderedText RenderedText { get; set; }
    public int GlyphIndex { get; set; }
    
    protected override IWidget Build(IBuildContext context)
    {
        var renderedText = RenderedText;
        var glyphIndex = GlyphIndex;

        if (renderedText.GlyphCount == 0)
            return null;
        
        var glyph = renderedText.GetGlyph(glyphIndex);
        
        var screenRect = glyph.ScreenRect;

        var caretRect = new Rect(screenRect.X + screenRect.Width, ScreenRect.Y + 20f, 3f, ScreenRect.Height - 40f);
        
        return new PanelWidget
        {
            ScreenRect = caretRect,
            Style = new PanelStyle
            {
                BackgroundColor = Color.FromHex(0xff00ff, 1f),
            },
        };
    }
}