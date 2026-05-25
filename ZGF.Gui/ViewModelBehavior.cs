namespace ZGF.Gui;

/// <summary>
/// Constructs a view model on attach and binds it to the view. The view model is disposed
/// on detach; State{T}/Derived{T} fields disposed by the VM clear their own subscribers, so
/// view-side subscriptions go dormant without explicit unsubscription. The view is expected
/// to only subscribe to its own VM's observables — for cross-cutting subscriptions
/// (MessageBus, shared State, etc.) push the subscription into the VM instead.
/// </summary>
public sealed class ViewModelBehavior<TVm> : IViewBehavior where TVm : IDisposable
{
    private readonly Func<Context, TVm> _factory;
    private readonly Action<TVm> _bind;
    private TVm? _vm;

    public ViewModelBehavior(Func<Context, TVm> factory, Action<TVm> bind)
    {
        _factory = factory;
        _bind = bind;
    }

    public void AttachToContext(View view, Context context)
    {
        _vm = _factory(context);
        _bind(_vm);
    }

    public void DetachFromContext(View view, Context context)
    {
        _vm?.Dispose();
        _vm = default;
    }
}
