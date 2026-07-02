using ZGF.Gui.Views;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Five-region layout: edge regions take their intrinsic size, <see cref="Center"/> fills
/// the rest. Builds a <see cref="BorderLayoutView"/>.
/// </summary>
public sealed record BorderLayout : Widget
{
    public IWidget? North { get; init; }
    public IWidget? South { get; init; }
    public IWidget? East { get; init; }
    public IWidget? West { get; init; }
    public IWidget? Center { get; init; }

    /// <summary>Vertical spacing between the North, Center, and South regions.</summary>
    public float VGap { get; init; }

    /// <summary>Horizontal spacing between the West, Center, and East regions.</summary>
    public float HGap { get; init; }

    protected override View CreateView(Context ctx) => new BorderLayoutView
    {
        VGap = VGap,
        HGap = HGap,
        North = North?.BuildView(ctx),
        South = South?.BuildView(ctx),
        East = East?.BuildView(ctx),
        West = West?.BuildView(ctx),
        Center = Center?.BuildView(ctx),
    };
}
