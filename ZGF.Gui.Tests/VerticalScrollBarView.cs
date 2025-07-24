using System.Data.SqlTypes;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarViewController : IKeyboardMouseController
{
    private readonly VerticalScrollBarView _view;

    public VerticalScrollBarViewController(VerticalScrollBarView view)
    {
        _view = view;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public View View => _view;
    
    public void OnMouseEnter(in MouseEnterEvent e)
    {
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        this.Blur();
    }

    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            Console.WriteLine("Clicked");
            _view.ScrollToPoint(e.Mouse.Point);
            return false;
        }
        return false;
    }
}

public sealed class VerticalScrollBarView : View
{
    private readonly VerticalScrollBarThumbView _thumbView;

    public event Action<float> ScrollPositionChanged
    {
        add => _thumbView.ScrollPositionChanged += value;
        remove => _thumbView.ScrollPositionChanged -= value;
    }

    public VerticalScrollBarView()
    {
        PreferredWidth = 25;

        _thumbView = new VerticalScrollBarThumbView();
        
        var slideArea = new RectView
        {
            BackgroundColor = 0xCECECE,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0x9C9C9C,
                Top = 0x9C9C9C,
                Right = 0xFFFFFF,
                Bottom = 0xFFFFFF
            },
            Children =
            {
                _thumbView
            }
        };
        
        AddChildToSelf(slideArea);

        Controller = new VerticalScrollBarViewController(this);
    }

    public float Scale
    {
        get => _thumbView.Scale;
        set => _thumbView.Scale = value;
    }

    public void SetNormalizedScrollPosition(float normalizedPosition)
    {
        _thumbView.SetScrollPositionNormalized(normalizedPosition);
    }

    public void Scroll(float deltaY)
    {
        _thumbView.Move(deltaY);
    }

    public void ScrollToTop()
    {
        _thumbView.ScrollToTop();
    }

    public void ScrollToPoint(PointF point)
    {
        _thumbView.ScrollToPoint(point);
    }
}