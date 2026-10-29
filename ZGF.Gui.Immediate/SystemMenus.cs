using ZGF.Desktop;
using ZGF.Gui.Hosting;

namespace ZGF.Gui.Immediate;

/// <summary>Records the menus a frame describes into the window system's menu bar, in place of a bar
/// drawn in the window. Each frame walks the menus recorded the frame before, changing only what
/// differs: whether an item is enabled or checked changes in place, read as the system shows the menu,
/// and the menus are shown again only once an entry is added, removed or renamed. An item the user
/// picks is told on the next frame, as the item records it.</summary>
public sealed class SystemMenus
{
    private readonly ISystemMenuBar _bar;
    private readonly Action _wake;
    private readonly Action<AppMenuItem> _onPicked;
    private readonly List<AppMenu> _menus = new();
    private readonly List<Level> _levels = new();
    private int _depth;
    private readonly HashSet<AppMenuItem> _picked = new();
    private int _menuCursor;
    private bool _changed;

    /// <summary>A menu being recorded and how far; kept from frame to frame, one per depth.</summary>
    private sealed class Level
    {
        public AppMenu Menu = null!;
        public int Cursor;
        public bool SeparatorAsked;
    }

    internal SystemMenus(ISystemMenuBar bar, Action wake)
    {
        _bar = bar;
        _wake = wake;
        _onPicked = Pick;
    }

    private void Pick(AppMenuItem item)
    {
        _picked.Add(item);
        _wake();
    }

    /// <summary>Starts recording the bar's menus, from the first.</summary>
    public void BeginBar()
    {
        _depth = 0;
        _menuCursor = 0;
    }

    /// <summary>Ends the bar: drops menus no longer recorded, and shows the menus again where they
    /// changed. Picks no item recorded are forgotten.</summary>
    public void EndBar()
    {
        if (_menuCursor < _menus.Count)
        {
            _menus.RemoveRange(_menuCursor, _menus.Count - _menuCursor);
            _changed = true;
        }
        _picked.Clear();
        if (!_changed)
            return;
        _changed = false;
        _bar.Show(_menus, _onPicked);
    }

    /// <summary>Starts recording a menu of the bar, or a submenu of the menu being recorded.</summary>
    public void BeginMenu(string key, string title, AppMenuRole role)
    {
        if (_depth == 0)
        {
            Enter(TakeMenu(key, title, role));
            return;
        }
        var parent = _levels[_depth - 1];
        TakeAskedSeparator(parent);
        if (Current(parent) is AppMenu reused && Matches(reused, key, title, role))
        {
            parent.Cursor++;
            Enter(reused);
            return;
        }
        var submenu = new AppMenu(key, title, role);
        Replace(parent, submenu);
        Enter(submenu);
    }

    private void Enter(AppMenu menu)
    {
        if (_depth == _levels.Count)
            _levels.Add(new Level());
        var level = _levels[_depth++];
        level.Menu = menu;
        level.Cursor = 0;
        level.SeparatorAsked = false;
    }

    private AppMenu TakeMenu(string key, string title, AppMenuRole role)
    {
        if (_menuCursor < _menus.Count && Matches(_menus[_menuCursor], key, title, role))
            return _menus[_menuCursor++];
        _menus.RemoveRange(_menuCursor, _menus.Count - _menuCursor);
        var menu = new AppMenu(key, title, role);
        _menus.Add(menu);
        _menuCursor++;
        _changed = true;
        return menu;
    }

    private static bool Matches(AppMenu menu, string key, string title, AppMenuRole role) =>
        menu.Key == key && menu.Title == title && menu.Role == role;

    /// <summary>Ends the menu begun last, dropping its entries no longer recorded.</summary>
    public void EndMenu()
    {
        var level = _levels[--_depth];
        var entries = level.Menu.Entries;
        if (level.Cursor >= entries.Count)
            return;
        entries.RemoveRange(level.Cursor, entries.Count - level.Cursor);
        _changed = true;
    }

    /// <summary>Records an item of the menu being recorded; true on the frame after the user picked it.</summary>
    public bool Item(string key, string title, Shortcut? shortcut, bool enabled, bool check,
        AppMenuSystemAction systemAction = AppMenuSystemAction.None)
    {
        var level = _levels[_depth - 1];
        TakeAskedSeparator(level);
        var item = Current(level) as AppMenuItem;
        if (item != null && item.Key == key && item.Title == title && item.Shortcut == shortcut && item.SystemAction == systemAction)
        {
            level.Cursor++;
        }
        else
        {
            item = new AppMenuItem(key, title, shortcut, systemAction);
            Replace(level, item);
        }
        item.IsEnabled = enabled;
        item.IsChecked = check;
        return _picked.Remove(item) && enabled;
    }

    /// <summary>Asks for a separator before the next entry of the menu being recorded. One asked for
    /// before the first entry, right after another, or with no entry after it is left out.</summary>
    public void Separator()
    {
        var level = _levels[_depth - 1];
        if (level.Cursor > 0)
            level.SeparatorAsked = true;
    }

    private void TakeAskedSeparator(Level level)
    {
        if (!level.SeparatorAsked)
            return;
        level.SeparatorAsked = false;
        if (Current(level) is AppMenuSeparator)
            level.Cursor++;
        else
            Replace(level, new AppMenuSeparator());
    }

    /// <summary>The entry recorded last frame where the level's cursor is, or null past its end.</summary>
    private static AppMenuEntry? Current(Level level)
    {
        var entries = level.Menu.Entries;
        return level.Cursor < entries.Count ? entries[level.Cursor] : null;
    }

    /// <summary>Puts <paramref name="entry"/> at the level's cursor in place of the entries from there
    /// on, which no longer match what is recorded, and moves the cursor past it.</summary>
    private void Replace(Level level, AppMenuEntry entry)
    {
        var entries = level.Menu.Entries;
        entries.RemoveRange(level.Cursor, entries.Count - level.Cursor);
        entries.Add(entry);
        level.Cursor++;
        _changed = true;
    }
}
