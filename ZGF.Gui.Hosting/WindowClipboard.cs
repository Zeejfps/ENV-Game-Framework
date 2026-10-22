using ZGF.Desktop;

namespace ZGF.Gui.Hosting;

// Backs the clipboard with the GLFW window's connection to the display server — the same
// mechanism every other GUI app uses (X11 selections / Wayland data-device on Linux, the
// native clipboard elsewhere). No external xclip/wl-copy processes, so nothing to install.
internal sealed class WindowClipboard(IWindow window) : IClipboard
{
    public void SetText(string text) => window.SetClipboardText(text);

    public string? GetText()
    {
        var text = window.GetClipboardText();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
