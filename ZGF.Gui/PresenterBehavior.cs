namespace ZGF.Gui;

public sealed class PresenterBehavior<T> : IViewBehavior where T : IDisposable
{
    private readonly Func<Context, T> _factory;
    private T? _presenter;

    public PresenterBehavior(Func<Context, T> factory)
    {
        _factory = factory;
    }

    public void AttachToContext(View view, Context context)
    {
        _presenter = _factory(context);
    }

    public void DetachFromContext(View view, Context context)
    {
        _presenter?.Dispose();
        _presenter = default;
    }
}
