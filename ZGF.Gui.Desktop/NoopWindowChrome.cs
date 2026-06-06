using ZGF.Desktop;

namespace ZGF.Gui.Desktop;

internal sealed class NoopWindowChrome : IWindowChrome
{
    public void SetTitleBarTheme(IWindow window, bool dark) { }
}