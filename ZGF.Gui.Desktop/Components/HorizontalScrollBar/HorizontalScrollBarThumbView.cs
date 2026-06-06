using ZGF.Geometry;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.HorizontalScrollBar;

public sealed class HorizontalScrollBarThumbView : MultiChildView
{
    public event Action<float>? ScrollPositionChanged;

    private float _scale = 0.5f;
    public float Scale
    {
        get => _scale;
        set => SetField(ref _scale, value);
    }

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

    private float _distanceToLeft;
    private float DistanceToLeft
    {
        get => _distanceToLeft;
        set => SetField(ref _distanceToLeft, value);
    }

    private int _maxDistanceToLeft;

    private readonly RectView _background;

    public HorizontalScrollBarThumbView()
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
        var width = WidthConstraint * Scale;

        _maxDistanceToLeft = Math.Max(0, (int)(WidthConstraint - width));

        if (_distanceToLeft < 0)
        {
            _distanceToLeft = 0;
        }
        else if (_distanceToLeft > _maxDistanceToLeft)
        {
            _distanceToLeft = _maxDistanceToLeft;
        }

        var left = LeftConstraint + _distanceToLeft;
        Position = new RectF
        {
            Left = left,
            Bottom = BottomConstraint,
            Width = width,
            Height = HeightConstraint,
        };

        var scrollPositionNormalized = _maxDistanceToLeft > 0 ? _distanceToLeft / _maxDistanceToLeft : 0f;
        ScrollPositionChanged?.Invoke(scrollPositionNormalized);
    }

    public void SetScrollPositionNormalized(float normalizedPosition)
    {
        if (float.IsNaN(normalizedPosition) || float.IsInfinity(normalizedPosition))
            normalizedPosition = 0f;
        DistanceToLeft = Math.Clamp(normalizedPosition, 0f, 1f) * _maxDistanceToLeft;
    }

    public void Move(float deltaX)
    {
        DistanceToLeft += deltaX;
    }

    public void ScrollToStart()
    {
        DistanceToLeft = 0;
    }

    public void ScrollToPoint(PointF point)
    {
        var width = WidthConstraint * Scale;
        var halfWidth = width * 0.5f;
        DistanceToLeft = point.X - LeftConstraint - halfWidth;
    }
}
