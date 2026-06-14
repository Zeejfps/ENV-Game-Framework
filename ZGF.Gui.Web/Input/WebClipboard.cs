using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using ZGF.Gui;

namespace ZGF.Gui.Web.Input;

/// <summary>
/// Browser clipboard via the async <c>navigator.clipboard</c> API, adapted to the
/// synchronous <see cref="IClipboard"/> contract.
///
/// The neutral interface is synchronous but the browser API is not: writes are
/// fire-and-forget (also cached locally), and reads return the last value the
/// async refresh fetched. <see cref="RefreshAsync"/> primes/updates that cache.
/// Browser clipboard reads also require a user gesture + permission, so a cold
/// <see cref="GetText"/> may lag the true system clipboard by one refresh.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed partial class WebClipboard : IClipboard
{
    private string _cached = string.Empty;

    public static async Task InitAsync()
    {
        await JSHost.ImportAsync("clipboard", "../clipboard.js");
    }

    public void SetText(string text)
    {
        _cached = text ?? string.Empty;
        WriteText(_cached); // fire-and-forget
    }

    public string? GetText()
    {
        // Kick an async refresh for next time; return the most recent known value now.
        _ = RefreshAsync();
        return _cached;
    }

    public async Task RefreshAsync()
    {
        try { _cached = await ReadText() ?? string.Empty; }
        catch { /* permission denied / no gesture — keep the cached value */ }
    }

    [JSImport("writeText", "clipboard")] private static partial void WriteText(string text);
    [JSImport("readText", "clipboard")] private static partial Task<string> ReadText();
}
