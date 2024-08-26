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

    private int m_SelectionStartIndex = 0;
    private int SelectionStartIndex
    {
        get => m_SelectionStartIndex;
        set
        {
            if (value < 0)
                value = 0;
            
            if (value >= m_StringBuilder.Length)
                value = m_StringBuilder.Length - 1;
            
            SetField(ref m_SelectionStartIndex, value);
        }
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
            HorizontalTextAlignment = TextAlignment.Start,
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
                        GlyphIndex = SelectionStartIndex,
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
            
            m_StringBuilder.Remove(SelectionStartIndex, 1);
            SelectionStartIndex--;
        }
        else if (key == KeyboardKey.LeftArrow)
        {
            SelectionStartIndex--;
        }
        else if (key == KeyboardKey.RightArrow)
        {
            SelectionStartIndex++;
        }
        else
        {
            m_StringBuilder.Append(key.ToString().ToLower());
            SelectionStartIndex++;
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
        var renderedTextBounds = renderedText.Bounds;
        var glyphIndex = GlyphIndex;

        if (renderedText.GlyphCount == 0)
            return null;
        
        var glyph = renderedText.GetGlyph(glyphIndex);
        
        var glyphRect = glyph.ScreenRect;

        var caretRect = new Rect(glyphRect.X + glyphRect.Width + 2f, renderedTextBounds.Y - 10f, 3f, renderedTextBounds.Height + 20f);
        
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