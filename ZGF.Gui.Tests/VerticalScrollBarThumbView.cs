using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : MultiChildView
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

    private float _distanceToTop;
    private float DistanceToTop
    {
        get => _distanceToTop;
        set => SetField(ref _distanceToTop, value);
    }

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
            StyleClasses =
            {
                "raised_panel"
            }
        };

        AddChildToSelf(_background);
    }

    protected override void OnLayoutSelf()
    {
        var height = MaxHeightConstraint * Scale;
        
        _maxDistanceToTop = (int)(TopConstraint - height - BottomConstraint);

        if (_distanceToTop < 0)
        {
            _distanceToTop = 0;
        }
        else if (_distanceToTop > _maxDistanceToTop)
        {
            _distanceToTop = _maxDistanceToTop;
        }
        
        var bottom = TopConstraint - _distanceToTop - height;
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = bottom,
            Width = MinWidthConstraint,
            Height = height,
        };

        var scrollPositionNormalized = _distanceToTop / _maxDistanceToTop;
        ScrollPositionChanged?.Invoke(scrollPositionNormalized);
    }

    public void SetScrollPositionNormalized(float normalizedPosition)
    {
        DistanceToTop = normalizedPosition * _maxDistanceToTop;
    }

    public void Move(float deltaY)
    {
        DistanceToTop -= deltaY;
    }

    public void ScrollToTop()
    {
        DistanceToTop = 0;
    }

    public void ScrollToPoint(PointF point)
    {
        var height = MaxHeightConstraint * Scale;
        var halfHeight = height * 0.5f;
        DistanceToTop = TopConstraint - point.Y - halfHeight;
    }
}