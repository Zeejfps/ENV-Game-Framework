using ZGF.Core;

namespace ZGF.Gui;

// Backs the clipboard with the GLFW window's connection to the display server — the same
// mechanism every other GUI app uses (X11 selections / Wayland data-device on Linux, the
// native clipboard elsewhere). No external xclip/wl-copy processes, so nothing to install.
public sealed class WindowClipboard : IClipboard
{
    private readonly IWindowedApp _app;

    public WindowClipboard(IWindowedApp app)
    {
        _app = app;
    }

    public void SetText(string text) => _app.MainWindow.SetClipboardText(text);

    public string? GetText()
    {
        var text = _app.MainWindow.GetClipboardText();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
