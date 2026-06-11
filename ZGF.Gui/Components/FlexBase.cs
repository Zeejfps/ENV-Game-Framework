using ZGF.Gui.Views;

namespace ZGF.Gui.Components;

public abstract record FlexBase : Widget
{
    public float Gap { get; init; }
    public MainAxisAlignment MainAxis { get; init; } = MainAxisAlignment.Start;
    public CrossAxisAlignment CrossAxis { get; init; } = CrossAxisAlignment.Start;
    public IWidget[] Children { get; init; } = [];

    protected abstract Axis Axis { get; }

    protected override View CreateView(Context ctx)
    {
        var v = new FlexView
        {
            Axis = Axis,
            Gap = Gap,
            MainAxisAlignment = MainAxis,
            CrossAxisAlignment = CrossAxis,
        };
        foreach (var child in Children)
            v.Children.Add(child.BuildView(ctx));
        return v;
    }
}