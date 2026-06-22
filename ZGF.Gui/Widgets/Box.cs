using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

public sealed record Box : Widget
{
    public Prop<uint> Background { get; init; }
    public Prop<BorderRadiusStyle> BorderRadius { get; init; }
    public Prop<BorderSizeStyle> BorderSize { get; init; }
    public Prop<BorderColorStyle> BorderColor { get; init; }
    public Prop<BoxShadowStyle> Shadow { get; init; }
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new RectView();
        Background.Apply(ctx, v,static (x, c) => x.BackgroundColor = c);
        BorderRadius.Apply(ctx, v,static (x, r) => x.BorderRadius = r);
        BorderSize.Apply(ctx, v,static (x, s) => x.BorderSize = s);
        BorderColor.Apply(ctx, v,static (x, c) => x.BorderColor = c);
        Shadow.Apply(ctx, v,static (x, s) => x.BoxShadow = s);
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}