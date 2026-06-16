using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Box : Widget
{
    public Prop<uint> Background { get; init; }
    public Prop<PaddingStyle> Padding { get; init; }
    public StyleValue<BorderRadiusStyle> BorderRadius { get; init; }
    public StyleValue<BorderSizeStyle> BorderSize { get; init; }
    public Prop<BorderColorStyle> BorderColor { get; init; }
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new RectView();
        Background.Apply(v, static (x, c) => x.BackgroundColor = c);
        Padding.Apply(v, static (x, p) => x.Padding = p);
        if (BorderRadius.IsSet) v.BorderRadius = BorderRadius;
        if (BorderSize.IsSet) v.BorderSize = BorderSize;
        BorderColor.Apply(v, static (x, c) => x.BorderColor = c);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}