namespace ZGF.Gui.Immediate;

/// <summary>The main canvas over a window's framebuffer: <see cref="Width"/> by <see cref="Height"/> UI points at
/// <see cref="DpiScale"/> pixels per point.</summary>
internal readonly record struct ZoomedCanvas(int Width, int Height, float DpiScale);

/// <summary>How the app's own zoom, <see cref="Ui.Scale"/>, sizes the main canvas: it multiplies the monitor's scale
/// and leaves the window's framebuffer as it is, so the canvas has fewer or more UI points over the same pixels.</summary>
internal static class AppZoom
{
    /// <summary>Whether a frame asking for <paramref name="requested"/> changes a canvas zoomed to <paramref name="current"/>.</summary>
    public static bool Changes(float requested, float current) => float.IsFinite(requested) && requested > 0f && requested != current;

    public static ZoomedCanvas Canvas(int framebufferWidth, int framebufferHeight, float monitorScale, float zoom)
    {
        var scale = (monitorScale > 0f ? monitorScale : 1f) * zoom;
        return new ZoomedCanvas(Points(framebufferWidth, scale), Points(framebufferHeight, scale), scale);
    }

    private static int Points(int pixels, float scale) => (int)MathF.Max(1f, MathF.Round(Math.Max(1, pixels) / scale));
}
