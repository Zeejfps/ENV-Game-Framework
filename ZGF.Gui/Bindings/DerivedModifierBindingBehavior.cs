using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Modifier toggle backed by a compute function whose observable reads are auto-tracked.
/// When any dependency invalidates, the function re-runs and the modifier is added/removed
/// to match. Lifecycle: <see cref="Derived{T}"/> created on attach, disposed on detach.
/// </summary>
internal sealed class DerivedModifierBindingBehavior : IViewBehavior
{
    private readonly View _view;
    private readonly string _name;
    private readonly Func<bool> _compute;
    private Derived<bool>? _derived;
    private IDisposable? _subscription;

    public DerivedModifierBindingBehavior(View view, string name, Func<bool> compute)
    {
        _view = view;
        _name = name;
        _compute = compute;
    }

    public void AttachToContext(View view, Context context)
    {
        _derived = new Derived<bool>(_compute);
        _subscription = _derived.Subscribe(Apply);
    }

    public void DetachFromContext(View view, Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        _derived?.Dispose();
        _derived = null;
    }

    private void Apply(bool active)
    {
        if (active)
            _view.StyleModifiers.Add(_name);
        else
            _view.StyleModifiers.Remove(_name);
    }
}
