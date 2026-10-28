using System.Runtime.Versioning;
using ZGF.Desktop;

namespace ZGF.Gui.Hosting;

/// <summary>The system clipboard of the window system an app runs on, owned by the host that
/// creates it.</summary>
internal abstract class PlatformClipboard : IClipboard, IDisposable
{
    public static PlatformClipboard Create(IWindowedApp app)
    {
        if (OperatingSystem.IsWindows())
            return new Win32Clipboard();
        if (OperatingSystem.IsMacOS())
            return new MacClipboard();
        if (OperatingSystem.IsLinux())
            return CreateLinuxClipboard(app);
        return new WindowClipboard(app.MainWindow);
    }

    [SupportedOSPlatform("linux")]
    private static PlatformClipboard CreateLinuxClipboard(IWindowedApp app)
    {
        if (app.WindowSystem != WindowSystem.X11)
            return new WindowClipboard(app.MainWindow);
        var clipboard = X11Clipboard.Connect();
        if (clipboard == null)
            return new WindowClipboard(app.MainWindow);
        return clipboard;
    }

    public abstract void SetText(string text);
    public abstract void SetHtml(string html, string text);
    public abstract string? GetText();

    /// <summary>None, but where the window system's clipboard is read for pictures: Windows only, so
    /// far.</summary>
    public virtual ClipboardImage? GetImage() => null;

    public virtual void Dispose() { }
}
