namespace ZGF.WebView.Cef;

/// <summary>Diagnostics for the web-view pipeline. Verbose tracing (init, browser creation, paints, the
/// message pump) is opt-in via <c>ZGF_CEF_DEBUG=1</c> and goes through <see cref="Write"/>; genuine
/// faults — a crashed render process, a failed navigation, a page JS error — always surface through
/// <see cref="Error"/> so a blank/frozen web view is never silent.</summary>
internal static class CefLog
{
    public static readonly bool Enabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZGF_CEF_DEBUG"));

    /// <summary>Verbose trace — only emitted when <c>ZGF_CEF_DEBUG</c> is set.</summary>
    public static void Write(string message)
    {
        if (Enabled)
            Console.WriteLine($"[zgf-cef] {message}");
    }

    /// <summary>A fault worth seeing regardless of the debug flag: render-process crash, load failure,
    /// page error. Always written to stderr so it stands out even in a normal run.</summary>
    public static void Error(string message) =>
        Console.Error.WriteLine($"[zgf-cef][ERROR] {message}");
}
