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

    /// <summary>Whether this frame asked for the window to come to the front.</summary>
    internal bool IsFrontRequested { get; private set; }

    /// <summary>Brings the window to the front once this frame is drawn: shown again when it is
    /// minimized, as it was before, and given the focus, as far as the platform lets an app take it.</summary>
    public void BringWindowToFront() => IsFrontRequested = true;

    private void BeginWindowState()
    {
        RequestedWindowState = null;
        IsFrontRequested = false;
    }
}
