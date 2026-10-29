using ZGF.Desktop;

namespace ZGF.Gui.Hosting;

/// <summary>Where the system puts a menu of the menu bar to its own use: macOS fills the
/// <see cref="Window"/> menu with the open windows, the <see cref="Services"/> menu with other apps'
/// services, and gives the <see cref="Help"/> menu a search of every item.</summary>
public enum AppMenuRole
{
    Standard,
    Window,
    Services,
    Help,
}

/// <summary>What the system itself does for an item, in place of the app: macOS hides and quits
/// the app and minimizes and zooms its windows.</summary>
public enum AppMenuSystemAction
{
    None,
    Hide,
    HideOthers,
    ShowAll,
    Quit,
    Minimize,
    Zoom,
    BringAllToFront,
}

/// <summary>An entry of the system's menu bar: a <see cref="AppMenu"/>, an <see cref="AppMenuItem"/>
/// or an <see cref="AppMenuSeparator"/>.</summary>
public abstract class AppMenuEntry
{
    private protected AppMenuEntry()
    {
    }
}

/// <summary>A menu of the bar, or a submenu, and its entries. The first menu of the bar is the
/// app's own, whatever its title.</summary>
public sealed class AppMenu(string key, string title, AppMenuRole role) : AppMenuEntry
{
    /// <summary>Names the menu among its siblings, from one menu bar to the next.</summary>
    public string Key { get; } = key;
    public string Title { get; } = title;
    public AppMenuRole Role { get; } = role;
    public List<AppMenuEntry> Entries { get; } = new();
}

/// <summary>An item the app acts on when picked, or the system acts on when it has a
/// <see cref="SystemAction"/>. <see cref="IsEnabled"/> and <see cref="IsChecked"/> change in place, as
/// the app's state does, and are read as the menu is shown.</summary>
public sealed class AppMenuItem(string key, string title, Shortcut? shortcut, AppMenuSystemAction systemAction) : AppMenuEntry
{
    /// <summary>Names the item among its siblings, from one menu bar to the next.</summary>
    public string Key { get; } = key;
    public string Title { get; } = title;

    /// <summary>The keys that pick the item; null for none.</summary>
    public Shortcut? Shortcut { get; } = shortcut;

    public AppMenuSystemAction SystemAction { get; } = systemAction;
    public bool IsEnabled { get; set; } = true;
    public bool IsChecked { get; set; }
}

public sealed class AppMenuSeparator : AppMenuEntry
{
}

/// <summary>The window system's own menu bar, which shows the menus the app gives it in place of a
/// bar inside the window: macOS's, at the top of the screen.</summary>
public interface ISystemMenuBar
{
    /// <summary>Shows <paramref name="menus"/> in place of what was shown, telling
    /// <paramref name="picked"/> of each of their items the user picks.</summary>
    void Show(IReadOnlyList<AppMenu> menus, Action<AppMenuItem> picked);
}
