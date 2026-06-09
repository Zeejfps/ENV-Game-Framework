using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Desktop.Platforms.Osx;

/// <summary>
///     Installs a native AppKit menu bar (NSMenu) for the application, replacing the
///     generic one GLFW creates by default. Standard items (Quit, Hide, Minimize, …) bind
///     to AppKit's built-in first-responder selectors and "just work" through the responder
///     chain. Custom items route through a synthesized Objective-C target class whose action
///     IMP is an [UnmanagedCallersOnly] static — the AOT-safe way to receive a callback from
///     Cocoa — which looks the handler up by the NSMenuItem's integer tag.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed unsafe class MacOsAppMenu : IAppMenu
{
    // NSEventModifierFlags (NSUInteger).
    private const ulong NSEventModifierFlagShift = 1UL << 17;
    private const ulong NSEventModifierFlagControl = 1UL << 18;
    private const ulong NSEventModifierFlagOption = 1UL << 19;
    private const ulong NSEventModifierFlagCommand = 1UL << 20;

    // Custom handlers, looked up by tag from the native (static) click trampoline. The app
    // menu lives for the whole process, so a static table is fine; entries are never removed.
    private static readonly Dictionary<long, Action> Handlers = new();
    private static long _nextTag = 1;

    private static IntPtr _targetInstance;

    public void Install(AppMenuBar menuBar)
    {
        var app = msg_IntPtr(Class("NSApplication"), Sel("sharedApplication"));
        if (app == IntPtr.Zero) return;

        EnsureTarget();

        var mainMenu = NewMenu("");
        foreach (var menu in menuBar.Menus)
        {
            var submenu = NewMenu(menu.Title);
            foreach (var item in menu.Items)
                msg_Void_IntPtr(submenu, Sel("addItem:"), BuildItem(item));

            // Each top-level menu hangs off a container item in the main menu.
            var container = msg_IntPtr(msg_IntPtr(Class("NSMenuItem"), Sel("alloc")),
                Sel("initWithTitle:action:keyEquivalent:"), NSString(menu.Title), IntPtr.Zero, NSString(""));
            msg_Void_IntPtr(container, Sel("setSubmenu:"), submenu);
            msg_Void_IntPtr(mainMenu, Sel("addItem:"), container);

            switch (menu.Role)
            {
                case AppMenuRole.Window: msg_Void_IntPtr(app, Sel("setWindowsMenu:"), submenu); break;
                case AppMenuRole.Services: msg_Void_IntPtr(app, Sel("setServicesMenu:"), submenu); break;
                case AppMenuRole.Help: msg_Void_IntPtr(app, Sel("setHelpMenu:"), submenu); break;
            }
        }

        msg_Void_IntPtr(app, Sel("setMainMenu:"), mainMenu);
    }

    private static IntPtr BuildItem(AppMenuItem item)
    {
        if (item.IsSeparator)
            return msg_IntPtr(Class("NSMenuItem"), Sel("separatorItem"));

        var action = ResolveAction(item);
        var nsItem = msg_IntPtr(msg_IntPtr(Class("NSMenuItem"), Sel("alloc")),
            Sel("initWithTitle:action:keyEquivalent:"), NSString(item.Title), action, NSString(item.KeyEquivalent));

        if (item.KeyEquivalent.Length > 0)
            msg_Void_ULong(nsItem, Sel("setKeyEquivalentModifierMask:"), MapModifiers(item.Modifiers));

        // Custom items target our synthesized object; the tag carries the handler key.
        // Standard items leave target nil so AppKit routes the selector up the responder chain.
        if (item.OnClick != null)
        {
            var tag = _nextTag++;
            Handlers[tag] = item.OnClick;
            msg_Void_Long(nsItem, Sel("setTag:"), tag);
            msg_Void_IntPtr(nsItem, Sel("setTarget:"), _targetInstance);
        }

        return nsItem;
    }

    private static IntPtr ResolveAction(AppMenuItem item)
    {
        if (item.OnClick != null) return Sel("zgfMenuAction:");

        return item.Standard switch
        {
            AppMenuStandardAction.About => Sel("orderFrontStandardAboutPanel:"),
            AppMenuStandardAction.Hide => Sel("hide:"),
            AppMenuStandardAction.HideOthers => Sel("hideOtherApplications:"),
            AppMenuStandardAction.ShowAll => Sel("unhideAllApplications:"),
            AppMenuStandardAction.Quit => Sel("terminate:"),
            AppMenuStandardAction.Minimize => Sel("performMiniaturize:"),
            AppMenuStandardAction.Zoom => Sel("performZoom:"),
            AppMenuStandardAction.Close => Sel("performClose:"),
            AppMenuStandardAction.BringAllToFront => Sel("arrangeInFront:"),
            AppMenuStandardAction.ToggleFullScreen => Sel("toggleFullScreen:"),
            _ => IntPtr.Zero,
        };
    }

    private static ulong MapModifiers(AppMenuModifiers m)
    {
        ulong mask = 0;
        if ((m & AppMenuModifiers.Command) != 0) mask |= NSEventModifierFlagCommand;
        if ((m & AppMenuModifiers.Shift) != 0) mask |= NSEventModifierFlagShift;
        if ((m & AppMenuModifiers.Option) != 0) mask |= NSEventModifierFlagOption;
        if ((m & AppMenuModifiers.Control) != 0) mask |= NSEventModifierFlagControl;
        return mask;
    }

    private static IntPtr NewMenu(string title)
    {
        var menu = msg_IntPtr(msg_IntPtr(Class("NSMenu"), Sel("alloc")), Sel("initWithTitle:"), NSString(title));
        // The submenu title alone drives whether AppKit auto-populates the standard window
        // list; keep auto-enable on so standard items grey out appropriately.
        return menu;
    }

    private static void EnsureTarget()
    {
        if (_targetInstance != IntPtr.Zero) return;

        var cls = objc_allocateClassPair(Class("NSObject"), "ZgfMenuTarget", UIntPtr.Zero);
        if (cls == IntPtr.Zero)
        {
            // Already registered (e.g. a prior Install) — reuse the existing class.
            cls = Class("ZgfMenuTarget");
        }
        else
        {
            // - (void)zgfMenuAction:(id)sender  ->  type encoding "v@:@"
            class_addMethod(cls, Sel("zgfMenuAction:"), (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnMenuClicked, "v@:@");
            objc_registerClassPair(cls);
        }

        _targetInstance = New(cls);
        Retain(_targetInstance);
    }

    [UnmanagedCallersOnly]
    private static void OnMenuClicked(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        // Called by Cocoa on the main thread (during event dispatch inside the GLFW poll).
        // Exceptions must never cross back into Objective-C, so swallow and log.
        try
        {
            var tag = msg_Long(sender, Sel("tag"));
            if (Handlers.TryGetValue(tag, out var handler))
                handler();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[AppMenu] menu action threw: {e}");
        }
    }

    // Builds an autoreleased NSString from a managed string via +[NSString stringWithUTF8String:].
    // UTF-8 (not ANSI) so non-ASCII glyphs in titles — e.g. the "…" in "Check for Updates…" —
    // survive the round-trip.
    private static IntPtr NSString(string value)
    {
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            return msg_IntPtr(Class("NSString"), Sel("stringWithUTF8String:"), utf8);
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }
}
