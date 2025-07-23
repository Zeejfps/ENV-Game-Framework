using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : View
{
    public float YOffset { get; set; }

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
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = TopConstraint - height - YOffset,
            Width = MinWidthConstraint,
            Height = height,
        };
    }
}