namespace ZGF.Gui;

/// <summary>
/// Binds a disposable's lifetime to the view: created (with the view's context) on attach,
/// disposed on detach. The generic building block behind <see cref="ViewBehaviorExtensions.Use"/> —
/// for view-scoped helpers like tooltips and scroll-sync controllers that need the context and a
/// dispose hook but are neither a ViewModel (no binding) nor an input controller.
/// </summary>
public sealed class ScopedBehavior<T> : IViewBehavior where T : IDisposable
{
    private readonly Func<Context, T> _factory;
    private T? _instance;

    public ScopedBehavior(Func<Context, T> factory)
    {
        _factory = factory;
    }

    public void AttachToContext(View view, Context context)
    {
        _instance = _factory(context);
    }

    public void DetachFromContext(View view, Context context)
    {
        _instance?.Dispose();
        _instance = default;
    }
}
