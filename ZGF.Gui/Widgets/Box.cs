using ZGF.Gui.Bindings;
using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Box : Widget
{
    public uint Background { get; init; }
    public PaddingStyle Padding { get; init; }
    public StyleValue<BorderRadiusStyle> BorderRadius { get; init; }
    public StyleValue<BorderSizeStyle> BorderSize { get; init; }
    public StyleValue<BorderColorStyle> BorderColor { get; init; }
    public IWidget[] Children { get; init; } = [];

    /// <summary>Auto-tracked background binding (hover/selection driven by VM state).</summary>
    public Func<uint>? BindBackground { get; init; }

    protected override View CreateView(Context ctx)
    {
        var v = new RectView { BackgroundColor = Background, Padding = Padding };
        if (BorderRadius.IsSet) v.BorderRadius = BorderRadius;
        if (BorderSize.IsSet) v.BorderSize = BorderSize;
        if (BorderColor.IsSet) v.BorderColor = BorderColor;
        if (BindBackground != null) v.BindBackgroundColor(BindBackground);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}