using System.Numerics;
using System.Text;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class TextField : StatefulWidget
{

    private StringBuilder m_StringBuilder = new();
    private readonly InputListenerController m_InputListenerController = new();
    
    protected override IWidget Build(IBuildContext context)
    {
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            OnKeyPressed = OnKeyPressed,
            Child = new StackWidget
            {
                Children =
                {
                    new PanelWidget
                    {
                        Style = new PanelStyle
                        {
                            BackgroundColor = Color.FromHex(0xff0000, 1f),
                            BorderRadius = new Vector4(5f, 5f, 5f, 5f),
                        }
                    },
                    new TextWidget(m_StringBuilder.ToString())
                    {
                        FontFamily = "Segoe UI",
                        Style = new TextStyle
                        {
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center,
                        }
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
            m_StringBuilder.Append(key);
        }
        
        SetDirty();
    }
}