namespace ZGF.Desktop;

/// <summary>
/// Decides where the main window sits on first show: honor a saved position when it still lands
/// on a connected monitor, otherwise center on the primary monitor. Kept separate from the
/// windowing backends so the clamping rules are pure and testable.
/// </summary>
public static class WindowPlacement
{
    /// <summary>
    /// Returns the top-left screen coordinate to place a <paramref name="windowWidth"/>×
    /// <paramref name="windowHeight"/> window at. A saved <paramref name="savedX"/>/<paramref name="savedY"/>
    /// is clamped fully onto whichever connected monitor it overlaps most; if it overlaps none
    /// (its monitor was unplugged or the layout changed) or none is supplied, the window is
    /// centered on the primary monitor. <paramref name="monitors"/> is the connected monitors'
    /// work areas with the primary first (as GLFW reports them).
    /// </summary>
    public static (int X, int Y) Compute(
        IReadOnlyList<MonitorWorkArea> monitors,
        int windowWidth, int windowHeight,
        int? savedX, int? savedY)
    {
        if (monitors.Count == 0)
            return (savedX ?? 0, savedY ?? 0);

        if (savedX is { } sx && savedY is { } sy &&
            ClampOntoMonitor(monitors, sx, sy, windowWidth, windowHeight) is { } clamped)
            return clamped;

        var primary = monitors[0];
        return (
            primary.X + (primary.Width - windowWidth) / 2,
            primary.Y + (primary.Height - windowHeight) / 2);
    }

    // Snaps the window fully inside the monitor it overlaps most, so a spot that hung off an edge
    // (or was on a now-removed monitor) comes back on-screen. Null when it overlaps no monitor at
    // all — the caller then centers instead.
    private static (int X, int Y)? ClampOntoMonitor(
        IReadOnlyList<MonitorWorkArea> monitors, int x, int y, int w, int h)
    {
        var best = monitors[0];
        var bestOverlap = 0L;
        foreach (var m in monitors)
        {
            var overlap = OverlapArea(x, y, w, h, m);
            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = m;
            }
        }

        if (bestOverlap <= 0)
            return null;

        // If the window is larger than the work area, Max keeps the lower bound so the top-left
        // stays pinned to the work area (title bar reachable) instead of clamping off the far edge.
        var nx = Math.Clamp(x, best.X, Math.Max(best.X, best.X + best.Width - w));
        var ny = Math.Clamp(y, best.Y, Math.Max(best.Y, best.Y + best.Height - h));
        return (nx, ny);
    }

    private static long OverlapArea(int x, int y, int w, int h, MonitorWorkArea m)
    {
        var ix = Math.Max(0, Math.Min(x + w, m.X + m.Width) - Math.Max(x, m.X));
        var iy = Math.Max(0, Math.Min(y + h, m.Y + m.Height) - Math.Max(y, m.Y));
        return (long)ix * iy;
    }
}
