namespace ZGF.Gui.Immediate;

/// <summary>How the window is shown: at its normal size and place, minimized, or maximized.</summary>
public enum WindowState
{
    Normal,
    Minimized,
    Maximized,
}

public abstract partial class Ui
{
    /// <summary>The state this frame asked the window for, taken once it is drawn; null when it asked
    /// for none.</summary>
    internal WindowState? RequestedWindowState { get; private set; }

    /// <summary>Minimizes the window once this frame is drawn.</summary>
    public void MinimizeWindow() => RequestedWindowState = WindowState.Minimized;

    /// <summary>Maximizes the window once this frame is drawn.</summary>
    public void MaximizeWindow() => RequestedWindowState = WindowState.Maximized;

    /// <summary>Brings the window back to its normal size and place once this frame is drawn.</summary>
    public void RestoreWindow() => RequestedWindowState = WindowState.Normal;

    private void BeginWindowState() => RequestedWindowState = null;
}
