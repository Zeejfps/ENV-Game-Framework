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

    private const long NSWindowAbove = 1;

    // NSWindowStyleMask flags (NSUInteger).
    private const ulong NSWindowStyleMaskResizable = 1UL << 3;
    private const ulong NSWindowStyleMaskFullScreen = 1UL << 14;

    // NSWindowButton values, for the title bar's close, minimize and zoom buttons.
    private static readonly IntPtr[] TitleBarButtons = [0, 1, 2];

    // The windows whose resizing SetInputEnabled turned off, to turn back on: only these, as a window
    // made fixed-size by its app has to stay so.
    private readonly HashSet<IntPtr> _resizingOff = new();

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

    // AppKit has no EnableWindow. This turns off what the window itself lets the user do: moving,
    // resizing and its title bar buttons. Its content goes on getting clicks, which the app has to
    // ignore, and a click still makes it key.
    public override void SetInputEnabled(IntPtr nsWindow, bool enabled)
    {
        if (nsWindow == IntPtr.Zero) return;

        msg_Void_Bool(nsWindow, Sel("setMovable:"), enabled);
        SetResizable(nsWindow, enabled);
        foreach (var button in TitleBarButtons)
        {
            // A window with no such button answers nil, and a message to nil does nothing.
            var view = msg_IntPtr(nsWindow, Sel("standardWindowButton:"), button);
            msg_Void_Bool(view, Sel("setEnabled:"), enabled);
        }
    }

    private void SetResizable(IntPtr nsWindow, bool resizable)
    {
        var mask = GetStyleMask(nsWindow, Sel("styleMask"));
        if (resizable)
        {
            if (_resizingOff.Remove(nsWindow))
                msg_Void_ULong(nsWindow, Sel("setStyleMask:"), mask | NSWindowStyleMaskResizable);
            return;
        }

        var canTurnOff = (mask & NSWindowStyleMaskResizable) != 0 && (mask & NSWindowStyleMaskFullScreen) == 0;
        if (!canTurnOff) return;
        _resizingOff.Add(nsWindow);
        msg_Void_ULong(nsWindow, Sel("setStyleMask:"), mask & ~NSWindowStyleMaskResizable);
    }

    // Keeps the window above its owner, as an owned window on Windows is, and also moves it along
    // with its owner, which SetInputEnabled keeps the user from dragging meanwhile. AppKit shows a
    // hidden window the moment it is added, so call this once the window has something to show.
    // Hiding or closing the window detaches it again.
    public override void SetOwner(IntPtr nsWindow, IntPtr owner)
    {
        if (nsWindow == IntPtr.Zero || owner == IntPtr.Zero) return;
        AddChildWindow(owner, Sel("addChildWindow:ordered:"), nsWindow, NSWindowAbove);
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

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern ulong GetStyleMask(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void AddChildWindow(IntPtr receiver, IntPtr selector, IntPtr child, long order);

    private static void SetLevel(IntPtr nsWindow, long level)
    {
        SetLevel(nsWindow, Sel("setLevel:"), level);
    }
}
