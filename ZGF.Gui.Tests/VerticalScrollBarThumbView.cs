using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : View
{
    private float _scrollPositionNormalized;
    public float ScrollPositionNormalized
    {
        get => _scrollPositionNormalized;
        set
        {
            if (value < 0)
                value = 0f;
            else if (value > 1f)
                value = 1f;

            _scrollPositionNormalized = value;
            SetDirty();
        }
    }

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

        var unclampedBottom = TopConstraint - height;
        var clampedScroll = ScrollPositionNormalized;
        var bottom = unclampedBottom * (1f - clampedScroll) + BottomConstraint * clampedScroll;

        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = bottom,
            Width = MinWidthConstraint,
            Height = height,
        };
    }
}