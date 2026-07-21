namespace ZGF.WebView.Cef;

/// <summary>Opt-in diagnostics for the web-view pipeline. Set <c>ZGF_CEF_DEBUG=1</c> to trace CEF
/// init, browser creation, paints and the message pump to the console.</summary>
internal static class CefLog
{
    public static readonly bool Enabled =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZGF_CEF_DEBUG"));

    public static void Write(string message)
    {
        if (Enabled)
            Console.WriteLine($"[zgf-cef] {message}");
    }
}
