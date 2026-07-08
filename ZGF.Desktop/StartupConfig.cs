namespace ZGF.Desktop;

public readonly struct StartupConfig
{
    public required int WindowWidth { get; init; }
    public required int WindowHeight { get; init; }
    public required string WindowTitle { get; init; }
    public bool IsUndecorated { get; init; }

    // Saved top-left screen position to restore the main window to. Null (the default) centers it
    // on the primary monitor. Clamped back on-screen at show time — see WindowPlacement.
    public int? WindowX { get; init; }
    public int? WindowY { get; init; }
}