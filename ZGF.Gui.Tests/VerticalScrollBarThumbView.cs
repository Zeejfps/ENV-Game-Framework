using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbView : View
{
    public float YOffset { get; set; } = 20;
    
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
        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = TopConstraint - PreferredHeight - YOffset,
            Width = MinWidthConstraint,
            Height = PreferredHeight,
        };
    }
}