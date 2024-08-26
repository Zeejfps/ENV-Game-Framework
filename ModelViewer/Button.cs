using System.Numerics;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class Button : StatefulWidget
{
    public Action OnPressed { get; set; }

    public Widget? Child { get; set; }
    
    private readonly InputListenerController m_InputListenerController = new();
    
    protected override IWidget Build(IBuildContext context)
    {
        var panel = new PanelWidget
        {
            ScreenRect = ScreenRect,
            Style = new PanelStyle
            {
                BackgroundColor = Color.FromHex(0xffffff, 1f),
                BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                BorderColor = Color.FromHex(0x6f6f6f, 1f),
                BorderSize = BorderSize.All(1f),
            }
        };

        var stack = new StackWidget();
        stack.Children.Add(panel);
        
        if (Child != null)
            stack.Children.Add(Child);
        
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            Child = stack
        };
    }
}
