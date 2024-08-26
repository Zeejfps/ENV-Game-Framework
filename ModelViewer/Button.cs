using System.Numerics;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class Button : StatefulWidget
{
    public Action OnPressed { get; set; }

    private readonly InputListenerController m_InputListenerController = new();
    
    protected override IWidget Build(IBuildContext context)
    {
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            Child = new PanelWidget
            {
                ScreenRect = ScreenRect,
                Style = new PanelStyle
                {
                    BackgroundColor = Color.FromHex(0xff00ff, 1f),
                    BorderRadius = new Vector4(5f, 5f, 5f, 5f)
                }
            }
        };
    }
}
