using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBar : Component
{
    private readonly Window _window;

    public WindowTitleBar(Window window)
    {
        _window = window;
        PreferredHeight = 30f;

        var background = new Panel
        {
            BackgroundColor = 0xCECECE,
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFF,
                Left = 0xFFFFFF,
                Right = 0x9C9C9C,
            },
            BorderSize = new BorderSizeStyle
            {
                Top = 1,
                Left = 1,
                Right = 1,
            },
            Padding = PaddingStyle.All(3)
        };

        var row = new FlexRow
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };
        background.Add(row);


        var button = new Panel();
        button.AddStyleClass("inset_panel");
        button.AddStyleClass("window_button");

        var button2 = new Panel();
        button2.AddStyleClass("inset_panel");
        button2.AddStyleClass("window_button");

        var titleLabel = new Label
        {
            Text = _window.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        row.Add(button);
        row.Add(titleLabel, new FlexStyle
        {
            Grow = 1f,
        });
        row.Add(button2);

        Add(background);

        IsInteractable = true;
    }

    private PointF _prevMousePosition;
    private bool _isHovered;
    private bool _isDragging;
    private bool _isLeftButtonPressed;

    protected override void OnMouseEnter()
    {
        Console.WriteLine($"OnMouseEnter: TitleBar - {_window.TitleText}");
        _isHovered = true;
        RequestFocus();
    }

    protected override void OnMouseExit()
    {
        Console.WriteLine($"OnMouseExit: TitleBar - {_window.TitleText}");
        _isHovered = false;
        if (!_isDragging)
        {
            _isLeftButtonPressed  = false;
            Blur();
        }
    }

    protected override bool OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        Console.WriteLine($"OnMouseButtonStateChanged: TitleBar - {_window.TitleText}");
        var button = e.Button;
        var state = e.State;

        if (button != MouseButton.Left)
            return false;

        if (state == InputState.Pressed)
        {
            _prevMousePosition = e.MousePoint;
            _isLeftButtonPressed = true;
            Console.WriteLine($"OnLeftButtonPressed: TitleBar - {_window.TitleText}");
            return false;
        }
        
        _isLeftButtonPressed = false;
        _isDragging = false;
        
        if (!_isHovered)
        {
            Console.WriteLine("Not hovered");
            Blur();
            return false;
        }
        
        return base.OnMouseButtonStateChanged(e);
    }

    protected override void OnFocusLost()
    {
        _isDragging = false;
        _isLeftButtonPressed = false;
        base.OnFocusLost();
    }

    public override bool CanReleaseFocus()
    {
        return !_isDragging;
    }

    protected override bool OnMouseMoved(MouseMoveEvent e)
    {
        if (!_isLeftButtonPressed)
            return false;

        var delta = e.MousePoint - _prevMousePosition;
        if (_isDragging)
        {
            _window.Move(delta.X, delta.Y);
            _prevMousePosition = e.MousePoint;
        }
        else if (delta.LengthSquared() > 1f)
        {
            _isDragging = true;
        }

        return base.OnMouseMoved(e);
    }

    public override string ToString()
    {
        return $"TitleBar - {_window.TitleText} - {ZIndex}";
    }
}