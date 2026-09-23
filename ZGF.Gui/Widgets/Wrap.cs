using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>A row whose children break onto further runs when they don't fit side by side.</summary>
public sealed record Wrap : Widget
{
    public float Gap { get; init; }
    public float RunGap { get; init; }
    public IWidget[] Children { get; init; } = [];

    protected override View CreateView(Context ctx)
    {
        var v = new WrapView { Gap = Gap, RunGap = RunGap };
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}
