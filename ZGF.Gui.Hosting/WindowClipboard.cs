using ZGF.Desktop;

namespace ZGF.Gui.Hosting;

// Backs the clipboard with the GLFW window's connection to the display server, where no native
// clipboard is written for it (a pure Wayland session). GLFW offers plain text only, so HTML is
// left behind.
internal sealed class WindowClipboard(IWindow window) : PlatformClipboard
{
    public override void SetText(string text) => window.SetClipboardText(text);

    public override void SetHtml(string html, string text) => window.SetClipboardText(text);

    public override string? GetText()
    {
        var text = window.GetClipboardText();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
