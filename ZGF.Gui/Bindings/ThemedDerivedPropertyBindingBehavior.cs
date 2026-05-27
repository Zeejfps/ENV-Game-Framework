using ZGF.Observable;

namespace ZGF.Gui.Bindings;

internal sealed class ThemedDerivedPropertyBindingBehavior<TView, TStyles, TProp> : IViewBehavior
    where TView : View
{
    private readonly TView _view;
    private readonly Func<TStyles, TProp> _select;
    private readonly Action<TView, TProp> _apply;
    private Derived<TProp>? _derived;
    private IDisposable? _subscription;

    public ThemedDerivedPropertyBindingBehavior(
        TView view,
        Func<TStyles, TProp> select,
        Action<TView, TProp> apply)
    {
        _view = view;
        _select = select;
        _apply = apply;
    }

    public void AttachToContext(View view, Context context)
    {
        var theme = context.Require<IThemeService<TStyles>>();
        _derived = new Derived<TProp>(() => _select(theme.Styles.Value));
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
