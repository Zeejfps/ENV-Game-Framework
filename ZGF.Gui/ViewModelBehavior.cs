namespace ZGF.Gui;

/// <summary>
/// Constructs a view model on mount and binds it to the view. The view model is disposed
/// on unmount; State{T}/Derived{T} fields disposed by the VM clear their own subscribers, so
/// view-side subscriptions go dormant without explicit unsubscription. The view is expected
/// to only subscribe to its own VM's observables — for cross-cutting subscriptions
/// (MessageBus, shared State, etc.) push the subscription into the VM instead.
/// </summary>
public sealed class ViewModelBehavior<TVm> : IViewBehavior where TVm : IDisposable
{
    private readonly Func<TVm> _factory;
    private readonly Action<TVm> _bind;
    private TVm? _vm;

    public ViewModelBehavior(Func<TVm> factory, Action<TVm> bind)
    {
        _factory = factory;
        _bind = bind;
    }

    public void Attach(View view)
    {
        _vm = _factory();
        _bind(_vm);
    }

    public void Detach(View view)
    {
        _vm?.Dispose();
        _vm = default;
    }
}
