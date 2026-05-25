namespace ZGF.Gui;

/// <summary>
/// Constructs a view model on attach and binds it to the view. The bind callback gets a
/// fresh <see cref="SubscriptionGroup"/> so view-side subscriptions to VM slices have a
/// matched lifecycle: the group is disposed on detach before the VM itself, so subscriber
/// handlers detach cleanly before the slices behind them shut down.
/// </summary>
public sealed class ViewModelBehavior<TVm> : IViewBehavior where TVm : IDisposable
{
    private readonly Func<Context, TVm> _factory;
    private readonly Action<TVm, SubscriptionGroup> _bind;
    private TVm? _vm;
    private SubscriptionGroup? _subs;

    public ViewModelBehavior(Func<Context, TVm> factory, Action<TVm, SubscriptionGroup> bind)
    {
        _factory = factory;
        _bind = bind;
    }

    public void AttachToContext(View view, Context context)
    {
        _vm = _factory(context);
        _subs = new SubscriptionGroup();
        _bind(_vm, _subs);
    }

    public void DetachFromContext(View view, Context context)
    {
        _subs?.Dispose();
        _subs = null;
        _vm?.Dispose();
        _vm = default;
    }
}
