using System.Runtime.Versioning;
using ZGF.Desktop;

namespace ZGF.Gui.Desktop.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsWindowChrome : IWindowChrome
{
    public void SetTitleBarTheme(IWindow window, bool dark) => window.SetTitleBarTheme(dark);
}
