using System.Numerics;
using OpenGLSandbox;

namespace ModelViewer;

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
    
    private readonly InputListenerController m_InputListenerController = new();

    private readonly string m_Text;
    
    public TextButton(string text)
    {
        m_Text = text;
    }
    
    protected override IWidget Build(IBuildContext context)
    {
        var normalBackgroundColor = Color.FromHex(0x2d2d2d, 1f);
        var pressedBackgroundColor = Color.FromHex(0x272727, 1f);
        
        var panel = new PanelWidget
        {
            ScreenRect = ScreenRect,
            Style = new PanelStyle
            {
                BackgroundColor = IsPressed 
                    ? pressedBackgroundColor 
                    : normalBackgroundColor,
                BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                BorderColor = Color.FromHex(0x353535, 1f),
                BorderSize = BorderSize.All(1f),
            }
        };

        var stack = new StackWidget();
        stack.Children.Add(panel);
        stack.Children.Add(new TextWidget(m_Text)
        {
            FontFamily = "Segoe UI",
            Style = new TextStyle
            {
                Color = Color.FromHex(0xffffff, 1f),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            },
        });
        
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
}
