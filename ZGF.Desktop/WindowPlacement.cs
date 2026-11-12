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
    /// <remarks>The position and size are the window's content area, as GLFW places a window; the frame the window
    /// system draws around it (<paramref name="frameLeft"/>, <paramref name="frameTop"/> for the title bar,
    /// <paramref name="frameRight"/>, <paramref name="frameBottom"/>) is kept on the monitor too, so a window saved at
    /// the top of the screen never opens with its title bar above it, out of reach.</remarks>
    public static (int X, int Y) Compute(
        IReadOnlyList<MonitorWorkArea> monitors,
        int windowWidth, int windowHeight,
        int? savedX, int? savedY,
        int frameLeft = 0, int frameTop = 0, int frameRight = 0, int frameBottom = 0)
    {
        if (monitors.Count == 0)
            return (savedX ?? 0, savedY ?? 0);

        var outerWidth = windowWidth + frameLeft + frameRight;
        var outerHeight = windowHeight + frameTop + frameBottom;
        if (savedX is { } sx && savedY is { } sy &&
            ClampOntoMonitor(monitors, sx - frameLeft, sy - frameTop, outerWidth, outerHeight) is { } clamped)
            return (clamped.X + frameLeft, clamped.Y + frameTop);

        var primary = monitors[0];
        return (
            primary.X + (primary.Width - outerWidth) / 2 + frameLeft,
            primary.Y + (primary.Height - outerHeight) / 2 + frameTop);
    }

    /// <summary>
    /// Returns the size a <paramref name="windowWidth"/>×<paramref name="windowHeight"/> window opens at so that it,
    /// with its frame (<paramref name="frameWidth"/> and <paramref name="frameHeight"/> added by the window system's
    /// borders and title bar), fits the work area of the monitor it opens on: the one a saved position overlaps most,
    /// else the primary. A window that fits keeps its size, so only a saved size from a bigger screen (or a monitor
    /// since scaled up) shrinks. Call before <see cref="Compute"/>, giving it the fitted size.
    /// </summary>
    public static (int Width, int Height) FitSize(
        IReadOnlyList<MonitorWorkArea> monitors,
        int windowWidth, int windowHeight,
        int? savedX, int? savedY,
        int frameWidth = 0, int frameHeight = 0)
    {
        if (monitors.Count == 0)
            return (windowWidth, windowHeight);

        var monitor = monitors[0];
        if (savedX is { } sx && savedY is { } sy)
        {
            var bestOverlap = 0L;
            foreach (var m in monitors)
            {
                var overlap = OverlapArea(sx, sy, windowWidth, windowHeight, m);
                if (overlap > bestOverlap)
                {
                    bestOverlap = overlap;
                    monitor = m;
                }
            }
        }

        var width = Math.Min(windowWidth, Math.Max(1, monitor.Width - frameWidth));
        var height = Math.Min(windowHeight, Math.Max(1, monitor.Height - frameHeight));
        return (width, height);
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
