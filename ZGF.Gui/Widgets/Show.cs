using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Reactive structural conditional: builds <see cref="Then"/> while <see cref="When"/> is true,
/// <see cref="Else"/> (or nothing) while it is false, swapping subtrees only when the boolean
/// flips. The outgoing branch unmounts — disposing its bindings — and the incoming one builds
/// fresh; an unchanged condition never rebuilds, so the live branch keeps its state.
/// <para>For a multi-source condition wrap the decision in a memo —
/// <c>When = new Derived&lt;bool&gt;(() =&gt; a.Value &amp;&amp; b.Value)</c> — so the swap still fires only on
/// a real change. To keep the data fresh without swapping, pass it into the branch as a
/// <see cref="Prop{T}"/> instead of reading it here.</para>
/// </summary>
public sealed record Show : Widget
{
    public required IReadable<bool> When { get; init; }
    public required Func<IWidget> Then { get; init; }
    public Func<IWidget>? Else { get; init; }

    protected override View CreateView(Context ctx)
    {
        var host = new ContainerView();
        host.Behaviors.Add(new SwapRegion<bool>(ctx, host, When,
            on => on ? Then() : Else?.Invoke() ?? Empty.Widget));
        return host;
    }
}
