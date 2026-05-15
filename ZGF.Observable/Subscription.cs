namespace ZGF.Observable;

internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose)
    {
        _onDispose = onDispose;
    }

    public void Dispose()
    {
        var d = _onDispose;
        _onDispose = null;
        d?.Invoke();
    }
}
