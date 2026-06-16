namespace ZGF.Gui;

/// <summary>
/// Binds a disposable's lifetime to the view's mounted period: created on mount, disposed on
/// unmount. The generic building block behind <see cref="ViewBehaviorExtensions.Use"/> — for
/// view-scoped helpers like tooltips and scroll-sync controllers that need a dispose hook but
/// are neither a ViewModel (no binding) nor an input controller. Dependencies are captured by
/// the factory closure at construction time.
/// </summary>
public sealed class ScopedBehavior<T> : IViewBehavior where T : IDisposable
{
    private readonly Func<T> _factory;
    private T? _instance;

    public ScopedBehavior(Func<T> factory)
    {
        _factory = factory;
    }

    public void Attach(View view)
    {
        _instance = _factory();
    }

    public void Detach(View view)
    {
        _instance?.Dispose();
        _instance = default;
    }
}
