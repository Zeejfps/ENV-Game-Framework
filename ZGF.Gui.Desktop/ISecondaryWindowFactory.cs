using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

public interface ISecondaryWindowFactory : IDisposable
{
    ISecondaryWindow Open(in SecondaryWindowRequest request);
}

public readonly struct SecondaryWindowRequest
{
    /// <summary>Builds the window's root view against the window's own per-window
    /// <see cref="Context"/> (canvas, input system, coordinates).</summary>
    public required Func<Context, View> BuildRoot { get; init; }
    public required string Title { get; init; }

    /// <summary>The window's size in screen coordinates — what the OS was last asked for and what a
    /// caller persists, not the logical size the content lays out in.</summary>
    public required int Width { get; init; }
    public required int Height { get; init; }

    /// <summary>Use client-drawn chrome over a transparent background.</summary>
    public bool IsUndecorated { get; init; }

    /// <summary>Center on the main window, limiting the size to its monitor's work area.
    /// Takes precedence over a saved X/Y position.</summary>
    public bool CenterOnMainWindow { get; init; }

    /// <summary>Optional saved top-left screen position. Clamped back onto a connected monitor
    /// before the window is shown (see <see cref="WindowPlacement"/>); when null the OS places
    /// the window.</summary>
    public int? X { get; init; }
    public int? Y { get; init; }
}

public interface ISecondaryWindow
{
    IWindow Window { get; }

    /// <summary>Raised after the window has been closed (native close button) and torn down.</summary>
    event Action Closed;

    /// <summary>Programmatically close and dispose the window.</summary>
    void Close();
}
