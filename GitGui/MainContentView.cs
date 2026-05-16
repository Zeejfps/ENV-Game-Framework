using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Shell for the main content area. Mounts one of two mode-specific views (history or local
/// changes) based on the observed <see cref="MainViewMode"/>. Both view instances are
/// constructed once and survive mode toggles — the active one is attached as a child;
/// the inactive one is fully detached so it stops receiving context-driven work.
/// </summary>
public sealed class MainContentView : MultiChildView
{
    private readonly HistoryView _history = new();
    private readonly LocalChangesView _localChanges = new();
    private View? _active;
    private IDisposable? _modeSubscription;

    protected override void OnAttachedToContext(Context context)
    {
        var mode = context.Get<State<MainViewMode>>();
        if (mode != null)
            _modeSubscription = mode.Subscribe(SetActive);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        _modeSubscription?.Dispose();
        _modeSubscription = null;
    }

    private void SetActive(MainViewMode mode)
    {
        var next = mode switch
        {
            MainViewMode.History => (View)_history,
            MainViewMode.LocalChanges => _localChanges,
            _ => _history,
        };

        if (ReferenceEquals(_active, next)) return;

        if (_active != null)
            RemoveChildFromSelf(_active);

        _active = next;
        AddChildToSelf(next);
    }

    protected override void OnLayoutChildren()
    {
        if (_active == null) return;
        var pos = Position;
        _active.LeftConstraint = pos.Left;
        _active.BottomConstraint = pos.Bottom;
        _active.MinWidthConstraint = pos.Width;
        _active.MaxWidthConstraint = pos.Width;
        _active.MaxHeightConstraint = pos.Height;
        _active.LayoutSelf();
    }
}
