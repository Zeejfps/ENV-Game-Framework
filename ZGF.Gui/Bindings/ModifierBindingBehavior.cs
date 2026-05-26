using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Toggles a named entry in <see cref="View.StyleModifiers"/> from an <see cref="IReadable{Boolean}"/>
/// source. True adds the modifier, false removes it. Subscription is tied to the view's context lifecycle.
/// </summary>
internal sealed class ModifierBindingBehavior : IViewBehavior
{
    private readonly View _view;
    private readonly string _name;
    private readonly IReadable<bool> _source;
    private IDisposable? _subscription;

    public ModifierBindingBehavior(View view, string name, IReadable<bool> source)
    {
        _view = view;
        _name = name;
        _source = source;
    }

    public void AttachToContext(View view, Context context)
    {
        _subscription = _source.Subscribe(Apply);
    }

    public void DetachFromContext(View view, Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        // Symmetric with Apply(true) — drop the modifier on detach so a re-parented view
        // doesn't carry a stale modifier from its previous lifetime. Reattach reads the
        // source's current value and re-adds if still true.
        _view.StyleModifiers.Remove(_name);
    }

    private void Apply(bool active)
    {
        if (active)
            _view.StyleModifiers.Add(_name);
        else
            _view.StyleModifiers.Remove(_name);
    }
}
