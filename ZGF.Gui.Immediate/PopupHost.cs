using ZGF.Desktop;

namespace ZGF.Gui.Immediate;

/// <summary>Shows <see cref="Ui.Popup"/> and <see cref="Ui.Window"/> content in windows of its own.
/// Rects are in the main window's UI space.</summary>
internal interface IPopupHost
{
    /// <summary>Where a popup that would rather sit at <paramref name="preferred"/>, and else at
    /// <paramref name="fallback"/>, ends up.</summary>
    UiRect Place(UiRect preferred, UiRect fallback);

    /// <summary>Shows popup <paramref name="id"/>, whose content is <paramref name="rect"/>, in a
    /// window covering <paramref name="surface"/> — the rect plus what it paints around itself, a
    /// shadow — this frame, and returns the canvas to draw it on, origin at the surface's top-left;
    /// null draws it over the main window. Only <paramref name="rect"/> keeps the pointer from what
    /// is under the popup.</summary>
    ICanvas? Show(int id, UiRect rect, UiRect surface, bool passThrough);

    /// <summary>Hides every popup now, before something modal of the platform's takes over.</summary>
    void HideAll();

    /// <summary>Shows window <paramref name="id"/> titled <paramref name="title"/> this frame, without a
    /// frame of the platform's and modal over the main window. It opens at <paramref name="openAt"/>,
    /// kept on a monitor, or else <paramref name="width"/> by <paramref name="height"/> points centered
    /// on the main window; then it is moved and sized to <paramref name="placeAt"/> when given. Returns
    /// its canvas, origin at its top-left, or null when the platform has no windows to give.</summary>
    WindowSurface? ShowWindow(int id, string title, float width, float height, WindowBounds? openAt, UiRect? placeAt);
}

/// <summary>A shown <see cref="IPopupHost.ShowWindow"/> window: its canvas, where it is in UI space
/// and on the screen, and whether the user asked to close it since the last frame.</summary>
internal readonly record struct WindowSurface(ICanvas Canvas, UiRect Rect, WindowBounds Bounds, bool CloseRequested);

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
