using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// Reactive structural switch: rebuilds its single child by running <see cref="Case"/> against
/// <see cref="Value"/>, swapping only when the value changes. This is also "keys without keys" —
/// memoize on whatever discriminator you want to swap on (<c>vm.Selected.Select(s =&gt; s.Id)</c>)
/// and pass the live data into the built branch as a <see cref="Prop{T}"/>.
/// <para>By default the outgoing branch unmounts (disposing its bindings) and the incoming one
/// builds fresh. Set <see cref="KeepAlive"/> to keep every visited branch mounted and merely
/// toggle which is shown — needed when a hidden branch's view model must keep listening (else it
/// reloads on every switch). Keep-alive caches one view per distinct value, so use it for small,
/// bounded value domains (enums), not open-ended keys.</para>
/// </summary>
public sealed record Switch<T> : Widget
{
    public required IReadable<T> Value { get; init; }
    public required Func<T, IWidget> Case { get; init; }
    public bool KeepAlive { get; init; }

    protected override View CreateView(Context ctx)
    {
        var host = new ContainerView();
        host.Behaviors.Add(new SwapRegion<T>(ctx, host, Value, Case, KeepAlive));
        return host;
    }
}
