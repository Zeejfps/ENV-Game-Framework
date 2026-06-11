using ZGF.Observable;

namespace ZGF.Gui.Bindings;

/// <summary>
/// Generic one-way property binding. Subscribes to a source observable on attach;
/// projects each emitted value through <paramref name="project"/> and applies it via
/// <paramref name="apply"/>. Subscription is tied to the view's context lifecycle.
/// </summary>
internal sealed class PropertyBindingBehavior<TView, TSource, TProp> : IViewBehavior
    where TView : View
{
    private readonly TView _view;
    private readonly IReadable<TSource> _source;
    private readonly Func<TSource, TProp> _project;
    private readonly Action<TView, TProp> _apply;
    private IDisposable? _subscription;

    public PropertyBindingBehavior(
        TView view,
        IReadable<TSource> source,
        Func<TSource, TProp> project,
        Action<TView, TProp> apply)
    {
        _view = view;
        _source = source;
        _project = project;
        _apply = apply;
    }

    public void Attach(View view)
    {
        _subscription = _source.Subscribe(s => _apply(_view, _project(s)));
    }

    public void Detach(View view)
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}
