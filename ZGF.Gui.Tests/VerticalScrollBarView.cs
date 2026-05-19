using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarView : MultiChildView
{
    private readonly VerticalScrollBarThumbView _thumbView;
    private readonly RectView _slideArea;

    public event Action<float> ScrollPositionChanged
    {
        add => _thumbView.ScrollPositionChanged += value;
        remove => _thumbView.ScrollPositionChanged -= value;
    }

    public VerticalScrollBarThumbView Thumb => _thumbView;

    public StyleValue<uint> TrackBackgroundColor
    {
        get => _slideArea.BackgroundColor;
        set => _slideArea.BackgroundColor = value;
    }

    public BorderColorStyle TrackBorderColor
    {
        get => _slideArea.BorderColor;
        set => _slideArea.BorderColor = value;
    }

    public BorderSizeStyle TrackBorderSize
    {
        get => _slideArea.BorderSize;
        set => _slideArea.BorderSize = value;
    }

    public VerticalScrollBarView()
    {
        PreferredWidth = 12;

        _thumbView = new VerticalScrollBarThumbView();

        _slideArea = new RectView
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

        AddChildToSelf(_slideArea);

        _thumbView.UseController(_ => new VerticalScrollBarThumbViewController(_thumbView));
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