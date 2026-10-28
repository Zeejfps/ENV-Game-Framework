using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Desktop;

[SupportedOSPlatform("macos")]
internal sealed class MacOsWindowPlatform : WindowPlatform
{
    // Outside-click capture and the non-client press watch are not implemented here: both need an
    // NSEvent monitor block trampoline. Menus rely on main-window-click and focus-change dismissal.

    // CGFloat for NSWindowLevel etc; on AppKit these are nullable ints exposed as NSInteger.
    private const long NSPopUpMenuWindowLevel = 101;

    // NSWindowCollectionBehavior flags (NSUInteger).
    private const ulong NSWindowCollectionBehaviorTransient = 1UL << 3;
    private const ulong NSWindowCollectionBehaviorIgnoresCycle = 1UL << 6;
    private const ulong NSWindowCollectionBehaviorCanJoinAllSpaces = 1UL << 0;

    public override void DecoratePopup(IntPtr nsWindow)
    {
        if (nsWindow == IntPtr.Zero) return;

        // The popup paints its own border and shadow. A window-server shadow would draw a second
        // outline around the window's bounds, the glass rim macOS gives shadowed windows.
        msg_Void_Bool(nsWindow, Sel("setHasShadow:"), false);
        SetLevel(nsWindow, NSPopUpMenuWindowLevel);
        msg_Void_Bool(nsWindow, Sel("setHidesOnDeactivate:"), false);
        msg_Void_ULong(nsWindow, Sel("setCollectionBehavior:"),
            NSWindowCollectionBehaviorTransient
            | NSWindowCollectionBehaviorIgnoresCycle
            | NSWindowCollectionBehaviorCanJoinAllSpaces);
    }

    public override void SetTitleBarTheme(IntPtr nsWindow, bool dark)
    {
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

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SetLevel(IntPtr receiver, IntPtr selector, long level);

    private static void SetLevel(IntPtr nsWindow, long level)
    {
        SetLevel(nsWindow, Sel("setLevel:"), level);
    }
}
