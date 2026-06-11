using ZGF.Observable;

namespace ZGF.Gui.Bindings;

internal sealed class ThemedDerivedPropertyBindingBehavior<TView, TStyles, TProp> : IViewBehavior
    where TView : View
{
    private readonly TView _view;
    private readonly IThemeService<TStyles> _theme;
    private readonly Func<TStyles, TProp> _select;
    private readonly Action<TView, TProp> _apply;
    private Derived<TProp>? _derived;
    private IDisposable? _subscription;

    public ThemedDerivedPropertyBindingBehavior(
        TView view,
        IThemeService<TStyles> theme,
        Func<TStyles, TProp> select,
        Action<TView, TProp> apply)
    {
        _view = view;
        _theme = theme;
        _select = select;
        _apply = apply;
    }

    public void Attach(View view)
    {
        _derived = new Derived<TProp>(() => _select(_theme.Styles.Value));
        _subscription = _derived.Subscribe(v => _apply(_view, v));
    }

    public void Detach(View view)
    {
        _subscription?.Dispose();
        _subscription = null;
        _derived?.Dispose();
        _derived = null;
    }
}
