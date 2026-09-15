namespace ZGF.Gui;

public sealed class ActionDisposable : IDisposable
{
    private Action? _cleanup;

    public ActionDisposable(Action cleanup) => _cleanup = cleanup;

    public void Dispose()
    {
        var c = _cleanup;
        _cleanup = null;
        c?.Invoke();
    }
}
