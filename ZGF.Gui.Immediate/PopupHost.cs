namespace ZGF.Gui.Immediate;

/// <summary>Shows <see cref="Ui.Popup"/> content in windows of its own. Rects are in the main
/// window's UI space.</summary>
internal interface IPopupHost
{
    /// <summary>Where a popup that would rather sit at <paramref name="preferred"/>, and else at
    /// <paramref name="fallback"/>, ends up.</summary>
    UiRect Place(UiRect preferred, UiRect fallback);

    /// <summary>Shows popup <paramref name="id"/> at <paramref name="rect"/> this frame and returns
    /// the canvas to draw it on, origin at the rect's top-left; null draws it over the main window.</summary>
    ICanvas? Show(int id, UiRect rect, bool passThrough);

    /// <summary>Hides every popup now, before something modal of the platform's takes over.</summary>
    void HideAll();
}

internal static class PopupPlacement
{
    /// <summary><paramref name="preferred"/> if it fits inside <paramref name="area"/>, else
    /// <paramref name="fallback"/> if that does, else <paramref name="preferred"/> pushed inside.</summary>
    public static UiRect Fit(UiRect preferred, UiRect fallback, UiRect area)
    {
        if (Inside(preferred, area)) return preferred;
        if (Inside(fallback, area)) return fallback;
        var x = MathF.Max(area.X, MathF.Min(preferred.X, area.Right - preferred.Width));
        var y = MathF.Max(area.Y, MathF.Min(preferred.Y, area.Bottom - preferred.Height));
        return preferred with { X = x, Y = y };
    }

    /// <summary><paramref name="rect"/> moved onto the device pixel grid of a
    /// <paramref name="dpiScale"/> surface, so a popup's pixels line up with its window's.</summary>
    public static UiRect SnapToPixels(UiRect rect, float dpiScale)
    {
        var s = dpiScale > 0f ? dpiScale : 1f;
        return rect with { X = MathF.Round(rect.X * s) / s, Y = MathF.Round(rect.Y * s) / s };
    }

    /// <summary>The window size, in screen coordinates, whose framebuffer exactly holds a canvas of
    /// <paramref name="points"/> at <paramref name="dpiScale"/>.</summary>
    public static int ScreenSize(int points, float dpiScale, float pixelsPerScreen) =>
        Math.Max(1, (int)MathF.Round(MathF.Round(points * dpiScale) / pixelsPerScreen));

    private static bool Inside(UiRect r, UiRect area) =>
        r.X >= area.X && r.Y >= area.Y && r.Right <= area.Right && r.Bottom <= area.Bottom;
}
