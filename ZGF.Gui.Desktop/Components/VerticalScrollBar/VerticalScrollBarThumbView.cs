using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.VerticalScrollBar;

public sealed class VerticalScrollBarThumbView : View
{
    public event Action<float>? ScrollPositionChanged;

    private float _scale = 0.5f;
    public float Scale
    {
        get => _scale;
        set => SetField(ref _scale, value);
    }

    // The thumb never shrinks below this many pixels (a tiny proportional thumb on a long list is unusable),
    // clamped to the track height. The travel math uses the clamped height so dragging stays accurate.
    private float _minHeight;
    public float MinHeight
    {
        get => _minHeight;
        set => SetField(ref _minHeight, value);
    }

    private float ThumbHeight() => Math.Max(HeightConstraint * Scale, Math.Min(_minHeight, HeightConstraint));

    private uint _idleBackgroundColor = 0xFFCECECE;
    public uint IdleBackgroundColor
    {
        get => _idleBackgroundColor;
        set
        {
            _idleBackgroundColor = value;
            if (!_isSelected)
                _background.BackgroundColor = value;
        }
    }

    private uint _hoveredBackgroundColor = 0xFFE2E2E2;
    public uint HoveredBackgroundColor
    {
        get => _hoveredBackgroundColor;
        set
        {
            _hoveredBackgroundColor = value;
            if (_isSelected)
                _background.BackgroundColor = value;
        }
    }

    public BorderColorStyle BorderColor
    {
        get => _background.BorderColor;
        set => _background.BorderColor = value;
    }

    public BorderSizeStyle BorderSize
    {
        get => _background.BorderSize;
        set => _background.BorderSize = value;
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
            _background.BackgroundColor = _isSelected ? _hoveredBackgroundColor : _idleBackgroundColor;
        }
    }

    // The thumb's position is the normalized 0..1 fraction, not an absolute pixel offset: the pixel
    // travel (_maxDistanceToTop) shrinks and grows with Scale, so a stored pixel offset would re-map
    // to a different fraction whenever the thumb is resized. _maxDistanceToTop is the last laid-out
    // travel, kept only to convert pixel drags/clicks back into a fraction.
    private float _normalized;
    private int _maxDistanceToTop;

    private readonly RectView _background;

    public VerticalScrollBarThumbView()
    {
        _background = new RectView
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
        };

        AddChildToSelf(_background);
    }

    protected override void OnLayoutSelf()
    {
        var height = ThumbHeight();

        _maxDistanceToTop = Math.Max(0, (int)(TopConstraint - height - BottomConstraint));

        // Derive pixels from the fraction; layout must never echo a fraction back to the consumer,
        // or a Scale-driven re-layout would drag the content to a stale position (feedback loop).
        var distanceToTop = _normalized * _maxDistanceToTop;
        var bottom = TopConstraint - distanceToTop - height;
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = bottom,
            Width = WidthConstraint,
            Height = height,
        };
    }

    // External sync (content -> thumb): set the fraction without notifying the consumer back.
    public void SetScrollPositionNormalized(float normalizedPosition)
    {
        SetNormalized(normalizedPosition, notify: false);
    }

    public void Move(float deltaY)
    {
        if (_maxDistanceToTop <= 0) return;
        SetNormalized(_normalized - deltaY / _maxDistanceToTop, notify: true);
    }

    public void ScrollToTop()
    {
        SetNormalized(0f, notify: true);
    }

    public void ScrollToPoint(PointF point)
    {
        if (_maxDistanceToTop <= 0) return;
        var height = ThumbHeight();
        var halfHeight = height * 0.5f;
        var distanceToTop = TopConstraint - point.Y - halfHeight;
        SetNormalized(distanceToTop / _maxDistanceToTop, notify: true);
    }

    // User-driven moves (drag, track click, scroll-to-top) notify so the content follows; layout-time
    // repositioning does not, which is what keeps the resize -> echo feedback loop from firing.
    private void SetNormalized(float value, bool notify)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) value = 0f;
        value = Math.Clamp(value, 0f, 1f);
        if (_normalized != value)
        {
            _normalized = value;
            SetDirty();
        }
        if (notify)
            ScrollPositionChanged?.Invoke(_normalized);
    }
}