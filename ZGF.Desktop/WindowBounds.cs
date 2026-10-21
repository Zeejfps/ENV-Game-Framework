namespace ZGF.Desktop;

/// <summary>Where a window sits and how big it is, in screen coordinates, as it restores when not
/// <see cref="Maximized"/> — what an app saves to reopen its window the way it was left.</summary>
public readonly record struct WindowBounds(int X, int Y, int Width, int Height, bool Maximized)
{
    /// <summary><paramref name="config"/> opening its main window at these bounds.</summary>
    public StartupConfig ApplyTo(StartupConfig config) => config with
    {
        WindowX = X,
        WindowY = Y,
        WindowWidth = Width,
        WindowHeight = Height,
        StartMaximized = Maximized,
    };
}
