namespace ZGF.Desktop;

public readonly struct StartupConfig
{
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public required string WindowTitle { get; init; }
    public bool IsUndecorated { get; init; }

    // Show the main window without taking OS focus, leaving the user's current window active. For
    // scripted/automated runs: the driver injects input directly, so it needs no focus, and stealing
    // it would let the user's real keystrokes land in whatever field the script just focused.
    public bool StartUnfocused { get; init; }

    // Opens the main window off the screen, drawing as usual, until the app shows it, as when it has something to get
    // ready first. StartMaximized then applies as it is shown.
    public bool StartHidden { get; init; }

    // Saved top-left screen position to restore the main window to. Null (the default) centers it
    // on the primary monitor. Clamped back on-screen at show time — see WindowPlacement.
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }

    // Opens the main window maximized; WindowX/WindowY/WindowWidth/WindowHeight are then the bounds
    // it restores to.
    public bool StartMaximized { get; init; }

    // Never shows or focuses the main window, and the run loop never draws it: for automation, which draws
    // every window offscreen when it asks. StartHidden and StartMaximized are then ignored.
    public bool Headless { get; init; }
}