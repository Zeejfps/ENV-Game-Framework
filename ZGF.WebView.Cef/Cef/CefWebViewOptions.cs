namespace ZGF.WebView.Cef;

public enum CefPaintPath
{
    /// <summary>Frames stay on the GPU as shared textures when the canvas can import them, else pixels.</summary>
    PreferAccelerated,
    /// <summary>Frames are always copied out as pixels (CEF's <c>OnPaint</c>).</summary>
    Software,
}

public sealed record CefWebViewOptions
{
    public CefPaintPath PaintPath { get; init; } = CefPaintPath.PreferAccelerated;

    /// <summary>The most frames per second CEF paints a page at; CEF caps it at 60.</summary>
    public int FrameRate { get; init; } = 60;

    /// <summary>Writes each web view's paints per second and their cost to stderr once a second.</summary>
    public bool ReportPaintStats { get; init; }
}
