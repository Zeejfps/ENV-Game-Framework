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

    // Saved top-left screen position to restore the main window to. Null (the default) centers it
    // on the primary monitor. Clamped back on-screen at show time — see WindowPlacement.
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }
}