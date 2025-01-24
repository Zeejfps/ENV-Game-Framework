using System.Numerics;
using EasyGameFramework.GUI;
using OpenGLSandbox;

namespace Bricks.RaylibBackend;

public sealed class TextButton : StatefulWidget
{
    private bool m_IsHovered;
    private bool IsHovered
    {
        get => m_IsHovered;
        set => SetField(ref m_IsHovered, value);
    }
    
    private bool m_IsPressed;

    private bool IsPressed
    {
        get => m_IsPressed;
        set => SetField(ref m_IsPressed, value);
    }
    
    public Action OnClicked { get; set; }
    
    private TextStyle _defaultTextStyle;

    private string _text;
    
    private readonly InputListenerController m_InputListenerController = new();
    
    public TextButton(string text)
    {
        _text = text;
        _defaultTextStyle = new TextStyle
        {
            Color = Color.FromHex(0xAFAFAF, 1f),
            FontScale = 30,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
    }

    protected override IWidget Build(IBuildContext context)
    {
        var normalBackgroundColor = Color.FromHex(0x2d2d2d, 1f);
        var pressedBackgroundColor = Color.FromHex(0x272727, 1f);
        
        var panel = new PanelWidget
        {
            Style = new PanelStyle
            {
                BackgroundColor = IsPressed 
                    ? pressedBackgroundColor 
                    : normalBackgroundColor,
                BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                //BorderColor = Color.FromHex(0x353535, 1f),
                BorderSize = BorderSize.All(1f),
            }
        };


        var text = new TextWidget(_text);
        if (IsHovered)
        {
            text.Style = _defaultTextStyle with
            {
                Color = Color.FromHex(0xFFFFFF, 1f)
            };
        }
        else
        {
            text.Style = _defaultTextStyle;
        }
        
        var stack = new StackWidget
        {
            ScreenRect = ScreenRect
        };
        stack.Children.Add(panel);
        stack.Children.Add(text);
        
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            Child = stack,
            OnPointerEnter = () => IsHovered = true,
            OnPointerExit = () => IsHovered = false,
            OnPointerPressed = () =>
            {
                IsPressed = true;
                OnClicked?.Invoke();
            },
            OnPointerReleased = () => IsPressed = false,
        };
    }

    public override Rect Measure(IBuildContext context)
    {
        return base.Measure(context);
    }
}