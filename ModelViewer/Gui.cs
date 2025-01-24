using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.GUI;
using OpenGLSandbox;

namespace ModelViewer;

public sealed class Gui : StatefulWidget
{
    public Gui(IWindow window)
    {
        Window = window;
        Window.Resized += Window_OnResized;
    }

    public override void Dispose()
    {
        Window.Resized -= Window_OnResized;
        base.Dispose();
    }

    private void Window_OnResized()
    {
        SetDirty();
    }

    private IWindow Window { get; }

    protected override IWidget BuildContent(IBuildContext context)
    {
        var window = Window;
        ScreenRect = new Rect(0f, 0f, window.ScreenWidth, window.ScreenHeight);

        var childSize = 400;
        
        return new PaddingWidget
        {
            ScreenRect = ScreenRect,
            Offsets = Offsets.All(10f),
            Child = new GridList
            {
                Children =
                {
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                    new CustomGridItemWidget
                    {
                        ScreenRect = new Rect(0f, 0f, childSize, childSize),
                    },
                }
            }
        };
    }
}

public sealed class GridList : StatefulWidget
{
    private readonly InputListenerController m_InputListenerController = new()
    {
        IsFocused = true
    };

    private float m_Offset = 0f;
    private int m_FocusedChildIndex = 0;
    public List<GridItemWidget> Children { get; } = new();
    
    protected override IWidget BuildContent(IBuildContext context)
    {
        var children = Children;
        
        var totalWidth = 0f;
        foreach (var child in children)
        {
            totalWidth += child.ScreenRect.Width;
        }
        
        var focusedChild = children[m_FocusedChildIndex];
        var focusedChildRect = focusedChild.ScreenRect;
        if (focusedChildRect.Left < ScreenRect.Right && focusedChildRect.Right > ScreenRect.Right)
        {
            var delta = focusedChildRect.Right - ScreenRect.Right;
            m_Offset -= delta;
        }
        else if (focusedChildRect.Right > ScreenRect.Left && focusedChildRect.Left < ScreenRect.Left)
        {
            var delta = ScreenRect.Left - focusedChildRect.Left;
            m_Offset += delta;
        }

        var x = m_Offset;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var rect = child.ScreenRect;
            rect.X = x;
            child.ScreenRect = rect;
            x += rect.Width + 10f;

            child.IsFocused = i == m_FocusedChildIndex;
        }
        
        return new InputListenerWidget(m_InputListenerController)
        {
            ScreenRect = ScreenRect,
            OnKeyPressed = OnKeyPressed,
            Child = new MultiChildWidget(children),
        };
    }

    private void OnKeyPressed(KeyboardKey key)
    {
        if (key == KeyboardKey.LeftArrow)
        {
            m_FocusedChildIndex--;
            if (m_FocusedChildIndex < 0)
                m_FocusedChildIndex = 0;
            
            SetDirty();
        }
        else if (key == KeyboardKey.RightArrow)
        {
            m_FocusedChildIndex++;
            if (m_FocusedChildIndex >= Children.Count)
                m_FocusedChildIndex = Children.Count - 1;
            
            SetDirty();
        }
    }
}

public abstract class GridItemWidget : StatefulWidget
{
    private bool m_IsFocused;
    public bool IsFocused
    {
        get => m_IsFocused;
        set => SetField(ref m_IsFocused, value);
    }
}

public sealed class CustomGridItemWidget : GridItemWidget
{
    protected override IWidget BuildContent(IBuildContext context)
    {
        var focusedPanelStyle = new PanelStyle
        {
            BackgroundColor = Color.FromHex(0x22ff22, 1f),
            BorderColor = Color.FromHex(0xff00ff, 1f),
            BorderSize = BorderSize.All(4f),
            BorderRadius = new Vector4(15f, 15f, 15f, 15f),
        };

        var normalPanelStyle = new PanelStyle
        {
            BackgroundColor = Color.FromHex(0x22ff22, 1f),
            BorderSize = BorderSize.All(0f),
            BorderRadius = new Vector4(15f, 15f, 15f, 15f),
        };

        return new PanelWidget
        {
            ScreenRect = ScreenRect,
            Style = IsFocused ? focusedPanelStyle : normalPanelStyle,
        };
    }
}