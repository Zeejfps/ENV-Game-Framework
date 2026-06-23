namespace ZGF.Gui.Widgets;

/// <summary>
/// Establishes the ambient UI writing direction for its subtree. Horizontal layout — a <c>Row</c>'s
/// main axis, the cross axis of a <c>Column</c>, and <c>BorderLayout</c>'s West/East edges — reads it
/// via <see cref="IsRtl"/> to mirror Start/End and child order when the UI is right-to-left, the
/// styling mirror of <see cref="Foreground"/>/<c>Theme.Color</c>. Absent a scope the direction is
/// left-to-right, so existing trees are unaffected.
/// </summary>
public sealed record UiDirection : Widget
{
    public required Prop<bool> Rtl { get; init; }
    public required IWidget Child { get; init; }

    /// <summary>The ambient right-to-left flag resolved from the build context; false when unscoped.</summary>
    public static Prop<bool> IsRtl =>
        Prop.Deferred(ctx => ctx.GetRegistered<Holder>() is { } h ? h.Rtl : (Prop<bool>)false);

    protected override IWidget Build(Context ctx) => new Provide<Holder>
    {
        Value = new Holder(Rtl),
        Child = Child,
    };

    internal sealed record Holder(Prop<bool> Rtl);
}
