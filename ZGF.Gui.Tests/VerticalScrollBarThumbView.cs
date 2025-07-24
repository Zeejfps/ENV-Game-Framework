using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : View
{
    public event Action<float>? ScrollPositionChanged;

    private float _scale = 0.5f;
    public float Scale
    {
        get => _scale;
        set => SetField(ref _scale, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            if (_isSelected)
            {
                _background.BackgroundColor = 0xE2E2E2;
            }
            else
            {
                _background.BackgroundColor = 0xCECECE;
            }
        }
    }

    private float _bottom;
    private float Bottom
    {
        get => _bottom;
        set => SetField(ref _bottom, value);
    }

    private readonly RectView _background;

    public VerticalScrollBarThumbView()
    {
        _background = new RectView
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
            StyleClasses =
            {
                "raised_panel"
            }
        };

        AddChildToSelf(_background);

        Controller = new VerticalScrollBarThumbViewController(this);
    }

    protected override void OnLayoutSelf()
    {
        var height = MaxHeightConstraint * Scale;

        if (_bottom + height > TopConstraint)
            _bottom = TopConstraint - height;

        if (_bottom < BottomConstraint)
            _bottom = BottomConstraint;

        var unclampedTop = TopConstraint - height;
        var scrollPositionNormalized = (unclampedTop - _bottom) / (unclampedTop - BottomConstraint);
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = _bottom,
            Width = MinWidthConstraint,
            Height = height,
        };

        ScrollPositionChanged?.Invoke(scrollPositionNormalized);
    }

    public void SetScrollPositionNormalized(float normalizedPosition)
    {
        var height = MaxHeightConstraint * Scale;
        var unclampedBottom = TopConstraint - height;
        var clampedScroll = normalizedPosition;
        Bottom = unclampedBottom * (1f - clampedScroll) + BottomConstraint * clampedScroll;
    }

    public void Move(float deltaY)
    {
        Bottom += deltaY;
    }

    public void ScrollToTop()
    {
        var height = MaxHeightConstraint * Scale;
        Bottom = TopConstraint - height;
    }
}