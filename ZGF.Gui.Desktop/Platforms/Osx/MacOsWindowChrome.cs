using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ZGF.Desktop;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui;

[SupportedOSPlatform("macos")]
public sealed class MacOsWindowChrome : IWindowChrome
{
    public void SetTitleBarTheme(IWindow window, bool dark)
    {
        var nsWindow = window.NativeHandle;
        if (nsWindow == IntPtr.Zero) return;

        // [window setAppearance:[NSAppearance appearanceNamed:@"NSAppearanceName(Dark)Aqua"]]
        var name = NSString(dark ? "NSAppearanceNameDarkAqua" : "NSAppearanceNameAqua");
        var appearance = msg_IntPtr(Class("NSAppearance"), Sel("appearanceNamed:"), name);
        msg_Void_IntPtr(nsWindow, Sel("setAppearance:"), appearance);
    }

    // Builds an autoreleased NSString from a managed string via
    // +[NSString stringWithUTF8String:].
    private static IntPtr NSString(string value)
    {
        var utf8 = Marshal.StringToHGlobalAnsi(value);
        try
        {
            return msg_IntPtr(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeHGlobal(utf8);
        }
    }
}
