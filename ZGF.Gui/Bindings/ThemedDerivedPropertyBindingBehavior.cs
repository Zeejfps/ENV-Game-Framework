namespace ZGF.Gui.Bindings;

internal sealed class ThemedDerivedPropertyBindingBehavior<TView, TStyles, TProp> : IViewBehavior
    where TView : View
{
    private readonly DerivedPropertyBindingBehavior<TView, TProp> _inner;

    public ThemedDerivedPropertyBindingBehavior(
        TView view,
        IThemeService<TStyles> theme,
        Func<TStyles, TProp> select,
        Action<TView, TProp> apply)
    {
        _inner = new DerivedPropertyBindingBehavior<TView, TProp>(view, () => select(theme.Styles.Value), apply);
    }

    public void Attach(View view) => _inner.Attach(view);

    public void Detach(View view) => _inner.Detach(view);
}
