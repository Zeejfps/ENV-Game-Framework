using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : View
{
    private float _scrollNormalized;
    public float ScrollNormalized
    {
        get => _scrollNormalized;
        set
        {
            if (value < 0)
                value = 0f;
            else if (value > 1f)
                value = 1f;
            
            _scrollNormalized = value;
            SetDirty();
        }
    }

    private float _scale = 0.5f;
    public float Scale
    {
        get => _scale;
        set => SetField(ref _scale, value);
    } 
    
    public VerticalScrollBarThumbView()
    {
        AddChildToSelf(new RectView
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
        });
    }

    protected override void OnLayoutSelf()
    {
        var height = MaxHeightConstraint * Scale;

        var unclampedBottom = TopConstraint - height;
        var clampedScroll = ScrollNormalized;
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