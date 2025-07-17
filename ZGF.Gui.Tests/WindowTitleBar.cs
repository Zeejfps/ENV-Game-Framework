using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class WindowTitleBar : Component
{
    private readonly Window _window;

    public WindowTitleBar(Window window)
    {
        _window = window;
        Constraints = new RectF
        {
            Height = 20f,
        };

        var background = new Panel
        {
            Style =
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
            }
        };

        var row = new FlexRow
        {
            Gap = 3,
            CrossAxisAlignment = CrossAxisAlignment.Stretch
        };
        background.Add(row);


        var button = new Panel
        {
            Constraints = new RectF
            {
                Width = 13,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button.AddStyleClass("inset_panel");

        var button2 = new Panel
        {
            Constraints = new RectF
            {
                Width = 13f,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button2.AddStyleClass("inset_panel");

        var button3 = new Panel
        {
            Constraints = new RectF
            {
                Width = 13f,
            },
            Style =
            {
                BackgroundColor = 0xFF00FF
            }
        };
        button3.AddStyleClass("inset_panel");

        var titleLabel = new Label(_window.TitleText)
        {
            HorizontalTextAlignment = TextAlignment.Center,
        };

        row.Add(button);
        row.Add(titleLabel, new FlexStyle
        {
            Grow = 1f,
        });
        row.Add(button2);
        row.Add(button3);

        Add(background);
    }

    private PointF _prevMousePosition;
    private bool _isHovered;
    private bool _isDragging;
    private bool _isLeftButtonPressed;

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        context.MouseInputSystem.EnableHover(this);
    }

    protected override void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnterEvent");
        _isHovered = true;
        Context?.MouseInputSystem.TryFocus(this);
    }

    protected override void OnMouseExit()
    {
        Console.WriteLine("OnMouseExitEvent");
        _isHovered = false;
        if (!_isDragging)
        {
            _isLeftButtonPressed  = false;
            Context?.MouseInputSystem.Blur(this);
        }
    }

    protected override void OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        var button = e.Button;
        var state = e.State;

        if (button != MouseButton.Left)
            return;

        if (state == InputState.Pressed)
        {
            _prevMousePosition = e.Position;
            _isLeftButtonPressed = true;
            _window.BringToFront();
        }
        else
        {
            _isLeftButtonPressed = false;
            _isDragging = false;

            if (!_isHovered)
            {
                Context?.MouseInputSystem.Blur(this);
            }
        }
    }

    protected override void OnMouseMoved(MouseMoveEvent e)
    {
        if (!_isLeftButtonPressed)
            return;

        var delta = e.MousePosition -  _prevMousePosition;
        if (_isDragging)
        {
            _window.Move(delta.X, delta.Y);
            _prevMousePosition = e.MousePosition;
        }
        else if (delta.LengthSquared() > 1f)
        {
            _isDragging = true;
        }
    }
}