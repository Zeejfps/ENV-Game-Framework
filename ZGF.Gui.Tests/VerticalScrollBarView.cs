using ZGF.Geometry;

namespace ZGF.Gui.Tests;

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
            BackgroundColor = 0xFFCECECE,
            BorderSize = BorderSizeStyle.All(1),
            BorderColor = new BorderColorStyle
            {
                Left = 0xFF9C9C9C,
                Top = 0xFF9C9C9C,
                Right = 0xFFFFFFFF,
                Bottom = 0xFFFFFFFF
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