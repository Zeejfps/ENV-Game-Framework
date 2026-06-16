using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Box : Widget
{
    public Prop<uint> Background { get; init; }
    public Prop<PaddingStyle> Padding { get; init; }
    public Prop<BorderRadiusStyle> BorderRadius { get; init; }
    public Prop<BorderSizeStyle> BorderSize { get; init; }
    public Prop<BorderColorStyle> BorderColor { get; init; }
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new RectView();
        Background.Apply(v, static (x, c) => x.BackgroundColor = c);
        Padding.Apply(v, static (x, p) => x.Padding = p);
        BorderRadius.Apply(v, static (x, r) => x.BorderRadius = r);
        BorderSize.Apply(v, static (x, s) => x.BorderSize = s);
        BorderColor.Apply(v, static (x, c) => x.BorderColor = c);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}