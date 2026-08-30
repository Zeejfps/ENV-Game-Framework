using ZGF.Gui.Views;
using ZGF.Observable;

namespace ZGF.Gui.Widgets;

/// <summary>
/// The single behavior behind <see cref="Show"/> and <see cref="Switch{T}"/>: subscribes to a
/// discriminator and rebuilds the host's single child only when that value actually changes.
/// Memoization is free — <see cref="State{T}"/>/<see cref="Derived{T}"/> only notify on a real
/// change — so a stable discriminator never rebuilds, and the live branch keeps its focus,
/// scroll and animation state.
/// <para>Default (swap) mode disposes the outgoing branch's bindings via unmount and builds the
/// incoming one fresh. <paramref name="keepAlive"/> mode instead caches each built branch in a slot
/// of its own and toggles the slot, so a hidden branch stays mounted and its subscriptions keep
/// running without anything out here writing to the branch's own root view.</para>
/// <para>The region owns its host's <see cref="View.IsVisible"/> — swap mode hides the host while it
/// has no branch to show — so an author's <see cref="Widget.Visible"/> is routed in through
/// <see cref="SetAuthorVisible"/> and combined with that, rather than the two writing over each
/// other.</para>
/// </summary>
internal sealed class SwapRegion<T> : IViewBehavior
{
    private readonly Context _ctx;
    private readonly ContainerView _host;
    private readonly IReadable<T> _key;
    private readonly Func<T, IWidget> _build;
    private readonly Dictionary<T, View>? _cache;

    private IDisposable? _subscription;
    private View? _current;
    private bool _hasContent = true;
    private bool _authorVisible = true;

    public SwapRegion(Context ctx, ContainerView host, IReadable<T> key, Func<T, IWidget> build, bool keepAlive = false)
    {
        _ctx = ctx;
        _host = host;
        _key = key;
        _build = build;
        if (keepAlive)
            _cache = new Dictionary<T, View>();
    }

    /// <summary>The author's <see cref="Widget.Visible"/> for this region, kept apart from whether the
    /// region has a branch to show. The host is visible only when both say so, and either side can
    /// change at any time — a constant prop lands before the first swap, a bound one after it.</summary>
    public void SetAuthorVisible(bool visible)
    {
        _authorVisible = visible;
        _host.IsVisible = _hasContent && _authorVisible;
    }

    public void Attach(View view) => _subscription = _key.Subscribe(Swap);

    public void Detach(View view)
    {
        _subscription?.Dispose();
        _subscription = null;

        // Swap mode drops the live branch so a remount rebuilds fresh and never double-mounts it.
        // Keep-alive leaves cached children in place; unmount detaches them, remount restores them.
        if (_cache == null && _current != null)
        {
            _host.Children.Remove(_current);
            _current = null;
        }
    }

    private void Swap(T key)
    {
        if (_cache != null)
        {
            KeepAliveSwap(key);
            return;
        }

        if (_current != null)
        {
            _host.Children.Remove(_current);
            _current = null;
        }

        var widget = _build(key);
        _hasContent = !ReferenceEquals(widget, Empty.Widget);
        _host.IsVisible = _hasContent && _authorVisible;
        if (!_hasContent)
            return;

        _current = widget.BuildView(_ctx);
        _host.Children.Add(_current);
    }

    // Each cached branch gets its own slot view, and the slot — never the branch's own root — is
    // what gets shown and hidden. A branch whose root is itself a swap region (a Show or Switch at
    // the top of its widget) owns that root's IsVisible and will set it back to true the next time
    // its own discriminator fires; toggling it from out here would mean two owners for one flag,
    // and a hidden branch reappearing over the visible one the moment its data changed.
    private void KeepAliveSwap(T key)
    {
        if (_current != null)
            _current.IsVisible = false;

        if (!_cache!.TryGetValue(key, out var slot))
        {
            var built = new ContainerView();
            built.Children.Add(_build(key).BuildView(_ctx));
            slot = built;
            _cache[key] = slot;
            _host.Children.Add(slot);
        }

        slot.IsVisible = true;
        _current = slot;
    }
}

/// <summary>
/// The container a <see cref="Show"/> or <see cref="Switch{T}"/> builds into. It carries the region
/// that owns its <see cref="View.IsVisible"/> so the widget can hand an authored
/// <see cref="Widget.Visible"/> to that owner, instead of a second writer setting the same flag and
/// the two taking turns overwriting each other.
/// </summary>
internal sealed class SwapHostView : ContainerView
{
    internal Action<bool>? SetAuthorVisible { get; set; }
}
