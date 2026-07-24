namespace ZGF.Desktop;

// Tracks whether the app as a whole holds OS focus, over the window list the app already keeps.
// Each backend app watches every window it creates; the list is read live on each recompute, so
// windows that have closed simply stop counting.
internal sealed class AppForegroundTracker
{
    private readonly IReadOnlyList<IWindow> _windows;

    public AppForegroundTracker(IReadOnlyList<IWindow> windows) => _windows = windows;

    // The app is being constructed because it is launching into the foreground.
    public bool IsForeground { get; private set; } = true;

    public event Action<bool>? Changed;

    public void Watch(IWindow window) => window.OnFocusChanged += _ => Recompute();

    private void Recompute()
    {
        var foreground = false;
        foreach (var window in _windows)
        {
            if (!window.IsFocused) continue;
            foreground = true;
            break;
        }
        if (foreground == IsForeground) return;
        IsForeground = foreground;
        Changed?.Invoke(foreground);
    }
}
