using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Property binding that takes a compute function instead of an existing observable.
/// The function's observable reads are auto-tracked; when any dependency invalidates,
/// the function re-runs and the result is applied. The internal <see cref="Derived{T}"/>
/// is owned by the behavior and disposed when the view detaches — no leaks.
/// </summary>
internal sealed class DerivedPropertyBindingBehavior<TView, TProp> : IViewBehavior
    where TView : View
{
    private readonly TView _view;
    private readonly Func<TProp> _compute;
    private readonly Action<TView, TProp> _apply;
    private Derived<TProp>? _derived;
    private IDisposable? _subscription;

    public DerivedPropertyBindingBehavior(
        TView view,
        Func<TProp> compute,
        Action<TView, TProp> apply)
    {
        _view = view;
        _compute = compute;
        _apply = apply;
    }

    public void AttachToContext(View view, Context context)
    {
        _derived = new Derived<TProp>(_compute);
        _subscription = _derived.Subscribe(v => _apply(_view, v));
    }

    public void DetachFromContext(View view, Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        _derived?.Dispose();
        _derived = null;
    }
}
