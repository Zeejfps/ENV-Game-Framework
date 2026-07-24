using ZGF.Desktop;
using ZGF.Observable;

namespace ZGF.Gui.Desktop;

/// <summary>
/// <see cref="IWindowedApp.IsForeground"/> as an observable, resolvable from the <see cref="Context"/>.
/// True while any of the app's windows holds OS focus; background work that should idle while the
/// user is in another application reads this.
/// </summary>
public interface IAppForeground : IReadable<bool>;

// A projection, not a copy: the app owns the value and this only republishes its event in the
// shape the rest of the GUI layer reads values in.
internal sealed class AppForeground : IAppForeground
{
    private readonly IWindowedApp _app;

    public AppForeground(IWindowedApp app) => _app = app;

    public bool Value => _app.IsForeground;

    public IDisposable Subscribe(Action<bool> handler)
    {
        handler(_app.IsForeground);
        _app.OnForegroundChanged += handler;
        return new Unsubscriber(() => _app.OnForegroundChanged -= handler);
    }

    private sealed class Unsubscriber(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
