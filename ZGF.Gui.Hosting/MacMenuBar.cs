using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static ZGF.Rendering.Metal.Objc;

namespace ZGF.Gui.Hosting;

/// <summary>macOS's menu bar, as an NSMenu built from the app's menus. The app's items target an
/// object of a class made here, whose methods AppKit calls back: to pick an item, and to ask, as it
/// shows a menu or matches a key, whether the item is enabled and checked, which reads the
/// <see cref="AppMenuItem"/> as it is then. Items the system acts on target nothing, so AppKit sends
/// them up its responder chain.</summary>
[SupportedOSPlatform("macos")]
internal sealed unsafe class MacMenuBar : ISystemMenuBar
{
    private const string TargetClassName = "ZgfMenuTarget";

    // NSEventModifierFlags.
    private const ulong ShiftMask = 1UL << 17;
    private const ulong ControlMask = 1UL << 18;
    private const ulong OptionMask = 1UL << 19;
    private const ulong CommandMask = 1UL << 20;

    // The key equivalents AppKit gives the keys that type no character.
    private const char UpArrow = '';
    private const char DownArrow = '';
    private const char LeftArrow = '';
    private const char RightArrow = '';
    private const char F1 = '';
    private const int FunctionKeys = 12;

    private const long StateOff = 0;
    private const long StateOn = 1;

    // AppKit calls back into static methods with only the target object in hand, so this is how a
    // callback finds the menu bar it targets. A menu bar lives as long as the app.
    private static readonly Dictionary<IntPtr, MacMenuBar> ByTarget = new();

    private readonly IntPtr _target;
    private readonly List<AppMenuItem> _items = new();
    private Action<AppMenuItem>? _picked;

    public MacMenuBar()
    {
        _target = New(TargetClass());
        ByTarget[_target] = this;
    }

    public void Show(IReadOnlyList<AppMenu> menus, Action<AppMenuItem> picked)
    {
        _picked = picked;
        _items.Clear();
        var pool = objc_autoreleasePoolPush();
        try
        {
            var app = msg_IntPtr(Class("NSApplication"), Sel("sharedApplication"));
            var bar = NewMenu("");
            foreach (var menu in menus)
                AddSubmenu(app, bar, menu);
            msg_Void_IntPtr(app, Sel("setMainMenu:"), bar);
            Release(bar);
        }
        finally
        {
            objc_autoreleasePoolPop(pool);
        }
    }

    private void AddSubmenu(IntPtr app, IntPtr parent, AppMenu menu)
    {
        var submenu = NewMenu(menu.Title);
        foreach (var entry in menu.Entries)
            Add(app, submenu, entry);
        var holder = NewItem(menu.Title, IntPtr.Zero, "");
        msg_Void_IntPtr(holder, Sel("setSubmenu:"), submenu);
        msg_Void_IntPtr(parent, Sel("addItem:"), holder);
        Release(holder);
        GiveRole(app, submenu, menu.Role);
        Release(submenu);
    }

    private static void GiveRole(IntPtr app, IntPtr submenu, AppMenuRole role)
    {
        switch (role)
        {
            case AppMenuRole.Standard:
                return;
            case AppMenuRole.Window:
                msg_Void_IntPtr(app, Sel("setWindowsMenu:"), submenu);
                return;
            case AppMenuRole.Services:
                msg_Void_IntPtr(app, Sel("setServicesMenu:"), submenu);
                return;
            case AppMenuRole.Help:
                msg_Void_IntPtr(app, Sel("setHelpMenu:"), submenu);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(role), role, null);
        }
    }

    private void Add(IntPtr app, IntPtr menu, AppMenuEntry entry)
    {
        switch (entry)
        {
            case AppMenu submenu:
                AddSubmenu(app, menu, submenu);
                return;
            case AppMenuItem item:
                var native = BuildItem(item);
                msg_Void_IntPtr(menu, Sel("addItem:"), native);
                Release(native);
                return;
            case AppMenuSeparator:
                msg_Void_IntPtr(menu, Sel("addItem:"), msg_IntPtr(Class("NSMenuItem"), Sel("separatorItem")));
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(entry), entry, null);
        }
    }

    private IntPtr BuildItem(AppMenuItem item)
    {
        var (key, modifiers) = KeyEquivalent(item.Shortcut);
        if (item.SystemAction != AppMenuSystemAction.None)
        {
            var system = NewItem(item.Title, SystemSelector(item.SystemAction), key);
            msg_Void_ULong(system, Sel("setKeyEquivalentModifierMask:"), modifiers);
            return system;
        }

        var native = NewItem(item.Title, Sel("zgfMenuAction:"), key);
        msg_Void_ULong(native, Sel("setKeyEquivalentModifierMask:"), modifiers);
        msg_Void_IntPtr(native, Sel("setTarget:"), _target);
        _items.Add(item);
        msg_Void_Long(native, Sel("setTag:"), _items.Count);
        return native;
    }

    private static IntPtr SystemSelector(AppMenuSystemAction action) => action switch
    {
        AppMenuSystemAction.Hide => Sel("hide:"),
        AppMenuSystemAction.HideOthers => Sel("hideOtherApplications:"),
        AppMenuSystemAction.ShowAll => Sel("unhideAllApplications:"),
        // GLFW's app delegate turns this into a request to close the windows, which the app
        // handles as it does any close.
        AppMenuSystemAction.Quit => Sel("terminate:"),
        AppMenuSystemAction.Minimize => Sel("performMiniaturize:"),
        AppMenuSystemAction.Zoom => Sel("performZoom:"),
        AppMenuSystemAction.BringAllToFront => Sel("arrangeInFront:"),
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    /// <summary>The key equivalent and modifier mask of <paramref name="shortcut"/>, Ctrl standing for
    /// Command and Alt for Option; none for no shortcut, a key this cannot name, or one with neither
    /// Command nor a function key, which a text field must still get, as Option+Left moves by word.</summary>
    internal static (string Key, ulong Modifiers) KeyEquivalent(string? shortcut)
    {
        if (string.IsNullOrEmpty(shortcut))
            return ("", 0);
        var parts = shortcut.Split('+');
        var key = KeyOf(parts[^1]);
        if (key == null)
            return ("", 0);
        ulong modifiers = 0;
        foreach (var part in parts.AsSpan(0, parts.Length - 1))
        {
            var mask = ModifierOf(part);
            if (mask == null)
                return ("", 0);
            modifiers |= mask.Value;
        }
        var isFunctionKey = key[0] >= F1 && key[0] < F1 + FunctionKeys;
        if ((modifiers & CommandMask) == 0 && !isFunctionKey)
            return ("", 0);
        return (key, modifiers);
    }

    private static string? KeyOf(string name)
    {
        if (name.Length == 1)
            return char.ToLowerInvariant(name[0]).ToString();
        return name switch
        {
            "Up" => UpArrow.ToString(),
            "Down" => DownArrow.ToString(),
            "Left" => LeftArrow.ToString(),
            "Right" => RightArrow.ToString(),
            _ => FunctionKeyOf(name),
        };
    }

    private static string? FunctionKeyOf(string name)
    {
        if (name[0] != 'F' || !int.TryParse(name.AsSpan(1), out var number))
            return null;
        if (number < 1 || number > FunctionKeys)
            return null;
        return ((char)(F1 + number - 1)).ToString();
    }

    private static ulong? ModifierOf(string name) => name switch
    {
        "Ctrl" or "Cmd" => CommandMask,
        "Shift" => ShiftMask,
        "Alt" or "Option" => OptionMask,
        "Control" => ControlMask,
        _ => null,
    };

    /// <summary>The item of this menu bar with <paramref name="tag"/>, or null for a tag of a menu
    /// bar shown before.</summary>
    private AppMenuItem? ItemOf(long tag)
    {
        if (tag < 1 || tag > _items.Count)
            return null;
        return _items[(int)tag - 1];
    }

    private static IntPtr TargetClass()
    {
        var existing = Class(TargetClassName);
        if (existing != IntPtr.Zero)
            return existing;
        var made = objc_allocateClassPair(Class("NSObject"), TargetClassName, UIntPtr.Zero);
        // - (void)zgfMenuAction:(NSMenuItem *)sender
        class_addMethod(made, Sel("zgfMenuAction:"), (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnPicked, "v@:@");
        // - (BOOL)validateMenuItem:(NSMenuItem *)item
        class_addMethod(made, Sel("validateMenuItem:"), (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, IntPtr, byte>)&OnValidate, "B@:@");
        objc_registerClassPair(made);
        return made;
    }

    // AppKit calls both on the main thread, while it handles events inside the window system's
    // poll. An exception must not unwind into Objective-C, so each is caught and written out.

    [UnmanagedCallersOnly]
    private static void OnPicked(IntPtr self, IntPtr command, IntPtr sender)
    {
        try
        {
            if (!ByTarget.TryGetValue(self, out var bar))
                return;
            if (bar.ItemOf(msg_Long(sender, Sel("tag"))) is { IsEnabled: true } item)
                bar._picked?.Invoke(item);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"A menu item failed: {e}");
        }
    }

    [UnmanagedCallersOnly]
    private static byte OnValidate(IntPtr self, IntPtr command, IntPtr native)
    {
        try
        {
            if (!ByTarget.TryGetValue(self, out var bar))
                return 0;
            if (bar.ItemOf(msg_Long(native, Sel("tag"))) is not { } item)
                return 0;
            msg_Void_Long(native, Sel("setState:"), item.IsChecked ? StateOn : StateOff);
            return item.IsEnabled ? (byte)1 : (byte)0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"A menu item could not be checked: {e}");
            return 0;
        }
    }

    private static IntPtr NewMenu(string title) =>
        msg_IntPtr(msg_IntPtr(Class("NSMenu"), Sel("alloc")), Sel("initWithTitle:"), NSString(title));

    private static IntPtr NewItem(string title, IntPtr action, string key) =>
        msg_IntPtr(msg_IntPtr(Class("NSMenuItem"), Sel("alloc")), Sel("initWithTitle:action:keyEquivalent:"),
            NSString(title), action, NSString(key));

    // An autoreleased NSString, via +[NSString stringWithUTF8String:].
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
