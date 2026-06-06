using ZGF.Core;

namespace ZGF.Gui;

/// <summary>
///     Platform hook for adjusting native window chrome (title bar) that GLFW
///     does not expose. Implementations live in the application layer because
///     they require OS-specific native calls (DWM on Windows, AppKit on macOS).
/// </summary>
public interface IWindowChrome
{
    /// <summary>
    ///     Switches the native title bar between dark and light appearance.
    /// </summary>
    /// <param name="window">The window whose title bar to theme; the implementation
    ///     reads <see cref="IWindow.NativeHandle"/> for the OS-native handle it needs.</param>
    /// <param name="dark"><c>true</c> for a dark title bar, <c>false</c> for light.</param>
    void SetTitleBarTheme(IWindow window, bool dark);
}
