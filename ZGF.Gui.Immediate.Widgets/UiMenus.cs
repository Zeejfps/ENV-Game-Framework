using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>An open <see cref="UiWidgets.MenuBar"/>: record its menus with
/// <see cref="UiWidgets.Menu"/>, then dispose.</summary>
public readonly struct MenuBar : IDisposable
{
    private readonly Ui.Scope _row;
    private readonly Ui.Scope _provided;
    private readonly UiWidgets.MenuBarState _state;

    internal MenuBar(Ui.Scope row, Ui.Scope provided, UiWidgets.MenuBarState state)
    {
        _row = row;
        _provided = provided;
        _state = state;
    }

    public void Dispose()
    {
        _provided.Dispose();
        _row.Dispose();
        if (!_state.OpenSeen)
            _state.Close();
    }
}

/// <summary>A menu from <see cref="UiWidgets.Menu"/>, <see cref="UiWidgets.ContextMenu(Ui, int)"/> or
/// <see cref="UiWidgets.SubMenu"/>. While <see cref="IsOpen"/>, record its items inside the scope;
/// dispose either way.</summary>
public readonly struct MenuScope : IDisposable
{
    private readonly Ui.Scope _layer;
    private readonly Ui.Scope _popup;
    private readonly Ui.Scope _scroll;
    private readonly Ui.Scope _provided;
    private readonly Ui.Scope _title;
    private readonly bool _hasTitle;
    private readonly UiWidgets.MenuPopup? _items;

    internal MenuScope(Ui.Scope title)
    {
        _title = title;
        _hasTitle = true;
    }

    internal MenuScope(Ui.Scope layer, Ui.Scope popup, Ui.Scope scroll, Ui.Scope provided, UiWidgets.MenuPopup items, Ui.Scope title, bool hasTitle)
    {
        IsOpen = true;
        _layer = layer;
        _popup = popup;
        _scroll = scroll;
        _provided = provided;
        _items = items;
        _title = title;
        _hasTitle = hasTitle;
    }

    public bool IsOpen { get; }

    public void Dispose()
    {
        if (IsOpen)
        {
            _provided.Dispose();
            _scroll.Dispose();
            _popup.Dispose();
            _layer.Dispose();
            _items!.EndItems();
        }
        if (_hasTitle)
            _title.Dispose();
    }
}

public static partial class UiWidgets
{
    private const int MenuSalt = 0x4D454E55;
    private const int MenuScrollSalt = 0x4D534352;

    /// <summary>The id of the popup the menu title, dropdown or submenu item with <paramref name="ownerId"/> opens.</summary>
    internal static int MenuPopupId(int ownerId) => HashCode.Combine(ownerId, MenuSalt);

    private static readonly KeyboardKey[] MenuKeySet =
    [
        KeyboardKey.DownArrow, KeyboardKey.UpArrow, KeyboardKey.Home, KeyboardKey.End,
        KeyboardKey.Enter, KeyboardKey.Space, KeyboardKey.RightArrow, KeyboardKey.LeftArrow, KeyboardKey.Tab,
    ];

    /// <summary>One open menu's items and keyboard state. Items are counted as they are recorded;
    /// keys act on last frame's items.</summary>
    internal abstract class MenuPopup
    {
        private List<bool> _enabled = new();
        private List<bool> _recording = new();
        private List<bool> _subs = new();
        private List<bool> _recordingSubs = new();
        private List<int> _ids = new();
        private List<int> _recordingIds = new();
        private int _owner;
        private bool _childSeen;
        private PointF _pointer = new(float.NaN, float.NaN);

        public int PopupId;
        public int Highlight = -1;
        public int HighlightFirstOwner;
        public int PickIndex = -1;
        public int OpenSub = -1;
        public bool SubMenuKeyed;
        public MenuPopup? OpenChild;

        public int ScrollId => HashCode.Combine(PopupId, MenuScrollSalt);

        /// <summary>Whether the pointer moved since the menu last looked, so hovering takes the
        /// highlight from the keys only when the user moves the mouse, not while it rests on an item.</summary>
        public bool PointerMoved { get; private set; }

        public void TrackPointer(PointF pointer)
        {
            PointerMoved = pointer != _pointer;
            _pointer = pointer;
        }

        public abstract void Close();

        public void BeginItems(int owner)
        {
            if (owner != _owner)
            {
                _owner = owner;
                _enabled.Clear();
                _subs.Clear();
                _ids.Clear();
                Highlight = -1;
                OpenSub = -1;
                OpenChild = null;
            }
            _recording.Clear();
            _recordingSubs.Clear();
            _recordingIds.Clear();
            _childSeen = false;
        }

        public int AddItem(int id, bool enabled, bool subMenu = false)
        {
            _recording.Add(enabled);
            _recordingSubs.Add(subMenu);
            _recordingIds.Add(id);
            return _recording.Count - 1;
        }

        public bool SubMenuAt(int index) => index >= 0 && index < _subs.Count && _subs[index];

        public int ItemId(int index) => index >= 0 && index < _ids.Count ? _ids[index] : 0;

        public void ChildSeen(MenuPopup child)
        {
            OpenChild = child;
            _childSeen = true;
        }

        public void EndItems()
        {
            (_enabled, _recording) = (_recording, _enabled);
            (_subs, _recordingSubs) = (_recordingSubs, _subs);
            (_ids, _recordingIds) = (_recordingIds, _ids);
            if (!_childSeen) OpenChild = null;
            PickIndex = -1;
            if (Highlight >= _enabled.Count) Highlight = -1;
            if (HighlightFirstOwner != 0 && HighlightFirstOwner == _owner)
            {
                HighlightFirstOwner = 0;
                Highlight = Step(-1, 1);
            }
        }

        public void Reset()
        {
            Highlight = -1;
            PickIndex = -1;
            OpenSub = -1;
            OpenChild = null;
        }

        public bool IsEnabled(int index) => index >= 0 && index < _enabled.Count && _enabled[index];

        public int Step(int from, int direction)
        {
            var count = _enabled.Count;
            if (count == 0) return -1;
            var index = from;
            for (var i = 0; i < count; i++)
            {
                index = index < 0 ? (direction > 0 ? 0 : count - 1) : (index + direction + count) % count;
                if (_enabled[index]) return index;
            }
            return -1;
        }

        public bool Contains(Ui ui, PointF pointer)
        {
            for (var menu = this; menu != null; menu = menu.OpenChild)
            {
                if (ui.TryGetRect(menu.PopupId, out var rect) && rect.Contains(pointer))
                    return true;
            }
            return false;
        }
    }

    internal sealed class MenuBarState : MenuPopup
    {
        private List<int> _titles = new();
        private List<int> _previousTitles = new();

        public int Open;
        public bool OpenSeen;

        public override void Close()
        {
            Open = 0;
            Reset();
        }

        public void BeginTitles()
        {
            (_titles, _previousTitles) = (_previousTitles, _titles);
            _titles.Clear();
        }

        public void AddTitle(int id) => _titles.Add(id);

        public int Neighbor(int id, int direction)
        {
            var index = _previousTitles.IndexOf(id);
            if (index < 0 || _previousTitles.Count == 0) return id;
            return _previousTitles[(index + direction + _previousTitles.Count) % _previousTitles.Count];
        }
    }

    private sealed class ContextMenuState : MenuPopup
    {
        public bool Open;
        public bool JustOpened;
        public PointF At;

        public override void Close()
        {
            Open = false;
            Reset();
        }
    }

    private sealed class SubMenuState : MenuPopup
    {
        public MenuPopup Parent = null!;

        public override void Close() => Parent.Close();
    }

    /// <summary>A row of menu titles; each <see cref="Menu"/> recorded inside opens a dropdown.
    /// Once one is open, hovering another title switches to it, and pressing a title then
    /// releasing over an item picks it. While open, Up/Down/Home/End move through the items,
    /// Enter or Space picks, Left/Right switch menus or open and close submenus, and Escape closes.
    /// A press outside or a pick closes it, and keyboard focus returns to where it was.</summary>
    public static MenuBar MenuBar(this Ui ui, string id, Box box = default)
    {
        var theme = ui.Theme;
        var barId = ui.GetId(id);
        var state = ui.GetState<MenuBarState>(HashCode.Combine(barId, MenuSalt));
        if (state.Open != 0 && ShouldCloseMenu(ui, barId, state))
            state.Close();
        state.OpenSeen = false;
        state.BeginTitles();

        var row = ui.Row(barId, box with
        {
            Height = box.Height ?? theme.MenuBarHeight,
            Gap = box.Gap ?? 0f,
            Padding = box.Padding ?? new Spacing(theme.GapTight, theme.GapTight / 2),
            Background = box.Background ?? theme.Surface,
        });
        return new MenuBar(row, ui.Provide(state), state);
    }

    /// <summary>A title in the enclosing <see cref="MenuBar"/> and its dropdown. Ids work as for
    /// <see cref="Button"/>.</summary>
    public static MenuScope Menu(this Ui ui, string label, string? id = null)
    {
        var bar = ui.Get<MenuBarState>() ?? throw new InvalidOperationException("A Menu must be recorded inside a MenuBar.");
        var theme = ui.Theme;
        var i = ui.Interact(id ?? label);
        bar.AddTitle(i.Id);
        if (i.Pressed)
        {
            if (bar.Open == i.Id)
                bar.Close();
            else
                bar.Open = i.Id;
        }
        else if (bar.Open != 0 && bar.Open != i.Id && ui.IsPointerOver(i.Id))
        {
            bar.Open = i.Id;
        }
        var open = bar.Open == i.Id;

        var title = ui.Box(i.Id, new Box
        {
            Height = Sizing.Grow(),
            Padding = new Spacing(theme.MenuItemPaddingX, 0f),
            AlignY = Align.Center,
            Radius = theme.CornerRadius,
            Background = open ? theme.Selection : i.Hovered ? theme.ControlHovered : 0u,
        });
        ui.Text(id != null ? label : Visible(ui, i.Id, label), color: open ? theme.SelectionText : theme.Text);
        if (!open)
            return new MenuScope(title);

        bar.OpenSeen = true;
        bar.PopupId = MenuPopupId(i.Id);
        bar.BeginItems(i.Id);
        ui.TryGetRect(i.Id, out var anchor);
        var menu = OpenMenu(ui, bar, anchor, title: title, hasTitle: true);
        if (MenuKeys(ui, bar, isSubMenu: false) is var direction and not 0)
        {
            bar.Open = bar.Neighbor(i.Id, direction);
            bar.HighlightFirstOwner = bar.Open;
        }
        return menu;
    }

    /// <summary>Opens the <see cref="ContextMenu(Ui, string)"/> with <paramref name="id"/>, in the same
    /// id scope, at <paramref name="at"/> or else the pointer.</summary>
    public static void OpenContextMenu(this Ui ui, string id, PointF? at = null) => ui.OpenContextMenu(ui.GetId(id), at);

    /// <summary>Opens the <see cref="ContextMenu(Ui, int)"/> with <paramref name="id"/> at
    /// <paramref name="at"/> or else the pointer. Take the id once with <see cref="Ui.GetId(string)"/>
    /// and pass it to both, and they match wherever each is recorded.</summary>
    public static void OpenContextMenu(this Ui ui, int id, PointF? at = null)
    {
        var state = ui.GetState<ContextMenuState>(HashCode.Combine(id, MenuSalt));
        state.Open = true;
        state.JustOpened = true;
        state.At = at ?? ui.Input.Pointer;
    }

    public static MenuScope ContextMenu(this Ui ui, string id) => ui.ContextMenu(ui.GetId(id));

    /// <summary>A menu shown where <see cref="OpenContextMenu(Ui, int, PointF?)"/> asked, until Escape,
    /// a press outside, or a pick closes it. Record it every frame. Keys work as in a
    /// <see cref="MenuBar"/>.</summary>
    public static MenuScope ContextMenu(this Ui ui, int id)
    {
        var state = ui.GetState<ContextMenuState>(HashCode.Combine(id, MenuSalt));
        state.PopupId = id;
        if (state.Open && !state.JustOpened && ShouldCloseMenu(ui, 0, state))
            state.Close();
        if (state.JustOpened)
            state.Reset();
        state.JustOpened = false;
        if (!state.Open)
            return default;

        state.BeginItems(id);
        var menu = OpenMenu(ui, state, new UiRect(state.At.X, state.At.Y, 0f, 0f));
        MenuKeys(ui, state, isSubMenu: false);
        return menu;
    }

    /// <summary>An item that opens a nested menu beside it when hovered, clicked, or entered with
    /// Right or Enter; Left or Escape closes just that menu. Ids work as for <see cref="Button"/>.</summary>
    public static MenuScope SubMenu(this Ui ui, string label, bool enabled = true, string? id = null)
    {
        var parent = ui.Get<MenuPopup>() ?? throw new InvalidOperationException("A SubMenu must be recorded inside a menu.");
        var theme = ui.Theme;
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        var index = parent.AddItem(i.Id, enabled, subMenu: true);
        if (enabled && ((parent.PointerMoved && ui.IsPointerOver(i.Id)) || i.Clicked))
        {
            parent.Highlight = index;
            parent.OpenSub = index;
        }
        var open = enabled && parent.OpenSub == index;
        var highlighted = enabled && (index == parent.Highlight || open);
        var color = highlighted ? theme.SelectionText : enabled ? theme.Text : theme.TextDisabled;

        using (ui.Row(i.Id, MenuRowBox(theme, highlighted)))
        {
            ui.Leaf(new Box { Width = theme.MenuIconColumnWidth, Height = 0f });
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: color);
            ui.Leaf(new Box { Width = Sizing.Grow(theme.MenuShortcutGap), Height = 0f });
            using (ui.Box(new Box { Width = theme.IconSize, Height = Sizing.Grow() }))
                ui.Paint((color, theme.IconStrokeWidth), static (p, s) => DrawDisclosure(p, false, s.color, s.IconStrokeWidth));
        }
        if (!open)
            return default;

        var popupId = MenuPopupId(i.Id);
        var state = ui.GetState<SubMenuState>(popupId);
        if (parent.OpenChild != state)
            state.Reset();
        if (parent.SubMenuKeyed)
        {
            parent.SubMenuKeyed = false;
            state.HighlightFirstOwner = popupId;
        }
        state.Parent = parent;
        state.PopupId = popupId;
        parent.ChildSeen(state);
        state.BeginItems(popupId);
        ui.TryGetRect(i.Id, out var row);
        var inset = theme.MenuPadding + theme.BorderWidth;
        var anchor = row with { Y = row.Y - inset, Height = row.Height + inset * 2 };
        var menu = OpenMenu(ui, state, anchor, inset, PopupSide.End);
        MenuKeys(ui, state, isSubMenu: true);
        return menu;
    }

    /// <summary>Returns true on the frame the item is picked, which also closes its menu.
    /// <paramref name="check"/> shows a check mark when true and leaves room for one when false;
    /// otherwise <paramref name="icon"/>, when given, sits where the mark would.
    /// <paramref name="shortcut"/> is only displayed. Ids work as for <see cref="Button"/>.</summary>
    public static bool MenuItem(this Ui ui, string label, string? shortcut = null, bool enabled = true, bool? check = null, string? id = null,
        UiIcon? icon = null) =>
        ui.MenuItem(label, shortcut, enabled, check == true ? MenuMark.Check : MenuMark.None, id, icon, null, keepOpen: false);

    /// <summary>One choice of a set: shows a dot when <paramref name="selected"/> and leaves room
    /// for one otherwise. Returns true on the frame it is picked, like <see cref="MenuItem(Ui, string, string?, bool, bool?, string?)"/>.</summary>
    public static bool MenuRadioItem(this Ui ui, string label, bool selected, string? shortcut = null, bool enabled = true, string? id = null) =>
        ui.MenuItem(label, shortcut, enabled, selected ? MenuMark.Dot : MenuMark.None, id, null, null, keepOpen: false);

    private enum MenuMark
    {
        None,
        Check,
        Dot,
    }

    private static bool MenuItem(this Ui ui, string label, string? shortcut, bool enabled, MenuMark mark, string? id, UiIcon? icon, UiIcon? labelIcon, bool keepOpen)
    {
        var theme = ui.Theme;
        var menu = ui.Get<MenuPopup>();
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        var index = menu?.AddItem(i.Id, enabled) ?? -1;
        var over = enabled && ui.IsPointerOver(i.Id);
        if (over && menu is { PointerMoved: true })
        {
            menu.Highlight = index;
            menu.OpenSub = -1;
        }
        var input = ui.Input;
        var picked = enabled && (i.Clicked
            || (over && input.WasReleased(PointerButton.Left) && !input.WasCancelled(PointerButton.Left))
            || (menu != null && index == menu.PickIndex));
        var highlighted = enabled && (i.Held || (menu != null ? index == menu.Highlight : i.Hovered));
        var color = highlighted ? theme.SelectionText : enabled ? theme.Text : theme.TextDisabled;

        using (ui.Row(i.Id, MenuRowBox(theme, highlighted)))
        {
            using (ui.Box(new Box { Width = theme.MenuIconColumnWidth, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
            {
                if (mark == MenuMark.None && icon is { } shown)
                {
                    ui.Icon(shown);
                }
                else
                {
                    using (ui.Box(new Box { Width = theme.IconSize, Height = theme.IconSize }))
                    {
                        if (mark == MenuMark.Check)
                            ui.Paint((color, theme.IconStrokeWidth), static (p, s) => DrawCheckmark(p, s.color, s.IconStrokeWidth));
                        else if (mark == MenuMark.Dot)
                            ui.Paint(color, static (p, c) => p.Circle(new PointF(p.Rect.X + p.Rect.Width / 2, p.Rect.Y + p.Rect.Height / 2), p.Rect.Height * 0.2f, c));
                    }
                }
            }
            if (labelIcon is { } beside)
            {
                using (ui.Box(new Box { Width = theme.MenuIconColumnWidth, Height = Sizing.Grow(), AlignY = Align.Center }))
                    ui.Icon(beside);
            }
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: color);
            ui.Leaf(new Box { Width = Sizing.Grow(theme.MenuShortcutGap), Height = 0f });
            if (shortcut != null)
                ui.Text(shortcut, color: enabled ? theme.TextMuted : theme.TextDisabled);
        }

        if (picked && !keepOpen)
            menu?.Close();
        return picked;
    }

    /// <summary>A checkable item: picking it flips <paramref name="value"/>. With
    /// <paramref name="keepOpen"/> the menu stays open, so several items can be flipped in a row.
    /// An <paramref name="icon"/> is drawn between the check and the label.</summary>
    public static bool MenuItem(this Ui ui, string label, ref bool value, string? shortcut = null, bool enabled = true, string? id = null,
        bool keepOpen = false, UiIcon? icon = null)
    {
        var picked = ui.MenuItem(label, shortcut, enabled, value ? MenuMark.Check : MenuMark.None, id, null, icon, keepOpen);
        if (picked) value = !value;
        return picked;
    }

    public static void MenuSeparator(this Ui ui)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.MenuItemPaddingX, theme.GapTight) }))
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.SeparatorWidth, Background = theme.Separator });
    }

    /// <summary>Records the popup of <paramref name="menu"/> at <paramref name="anchor"/>: an input
    /// layer holding a column of items that scrolls once taller than <see cref="UiTheme.MenuMaxHeight"/>.</summary>
    private static MenuScope OpenMenu(Ui ui, MenuPopup menu, UiRect anchor, float gap = 0f, PopupSide side = PopupSide.Below, Ui.Scope title = default,
        bool hasTitle = false)
    {
        var theme = ui.Theme;
        menu.TrackPointer(ui.Input.Pointer);
        var layer = ui.Layer(menu.PopupId);
        var popup = ui.Popup(menu.PopupId, anchor, MenuBox(theme), gap: gap, side: side);
        var scroll = ui.Scroll(menu.ScrollId, new Box { Width = Sizing.Grow(), Height = Sizing.Fit(max: theme.MenuMaxHeight), Gap = 0f });
        var provided = ui.Provide(menu);
        return new MenuScope(layer, popup, scroll, provided, menu, title, hasTitle);
    }

    private static Box MenuBox(UiTheme theme) => new()
    {
        Width = Sizing.Fit(theme.MenuMinWidth),
        Padding = theme.MenuPadding,
        Background = theme.SurfaceRaised,
        BorderColor = theme.Border,
        BorderWidth = theme.BorderWidth,
        Radius = theme.PopupRadius,
        Shadow = theme.PopupShadow,
    };

    private static Box MenuRowBox(UiTheme theme, bool highlighted) => new()
    {
        Width = Sizing.Grow(),
        Height = theme.MenuItemHeight,
        Gap = 0f,
        Padding = new Spacing(0f, 0f, theme.MenuItemPaddingX, 0f),
        Radius = theme.CornerRadius,
        Background = highlighted ? theme.Selection : 0u,
    };

    /// <summary>Acts on this frame's keys for <paramref name="menu"/>, which only reach it while it is
    /// the topmost menu. Returns -1 or 1 when the user asked for the previous or next menu of a bar.</summary>
    private static int MenuKeys(Ui ui, MenuPopup menu, bool isSubMenu)
    {
        var input = ui.Input;
        if (input.TakeKey(KeyboardKey.Escape))
        {
            if (isSubMenu)
                CloseSubMenu(menu);
            else
                menu.Close();
            return 0;
        }

        var switchTo = 0;
        var tab = 0;
        var highlight = menu.Highlight;
        foreach (var stroke in input.Keys)
        {
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            switch (stroke.Key)
            {
                case KeyboardKey.Tab:
                    tab = (stroke.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1;
                    break;
                case KeyboardKey.DownArrow:
                    menu.Highlight = menu.Step(menu.Highlight, 1);
                    break;
                case KeyboardKey.UpArrow:
                    menu.Highlight = menu.Step(menu.Highlight, -1);
                    break;
                case KeyboardKey.Home:
                    menu.Highlight = menu.Step(-1, 1);
                    break;
                case KeyboardKey.End:
                    menu.Highlight = menu.Step(-1, -1);
                    break;
                case KeyboardKey.Enter:
                case KeyboardKey.Space:
                    if (!menu.IsEnabled(menu.Highlight)) break;
                    if (menu.SubMenuAt(menu.Highlight))
                        OpenSubMenuByKey(menu);
                    else
                        menu.PickIndex = menu.Highlight;
                    break;
                case KeyboardKey.RightArrow:
                    if (menu.IsEnabled(menu.Highlight) && menu.SubMenuAt(menu.Highlight))
                        OpenSubMenuByKey(menu);
                    else if (!isSubMenu)
                        switchTo = 1;
                    break;
                case KeyboardKey.LeftArrow:
                    if (isSubMenu)
                        CloseSubMenu(menu);
                    else
                        switchTo = -1;
                    break;
            }
        }
        foreach (var key in MenuKeySet)
            input.TakeKey(key);
        if (tab != 0)
        {
            menu.Close();
            ui.MoveFocus(tab);
            return 0;
        }
        if (menu.Highlight != highlight && menu.ItemId(menu.Highlight) is var item and not 0)
            ui.ScrollIntoView(menu.ScrollId, item);
        return switchTo;
    }

    private static void OpenSubMenuByKey(MenuPopup menu)
    {
        menu.OpenSub = menu.Highlight;
        menu.SubMenuKeyed = true;
    }

    private static void CloseSubMenu(MenuPopup menu)
    {
        if (menu is SubMenuState sub)
        {
            sub.Parent.OpenSub = -1;
            sub.Reset();
        }
    }

    private static bool ShouldCloseMenu(Ui ui, int ownerId, MenuPopup menu)
    {
        var input = ui.Input;
        if (input.PopupsDismissed) return true;
        if (!input.WasPressed(PointerButton.Left) && !input.WasPressed(PointerButton.Right) && !input.WasPressed(PointerButton.Middle))
            return false;
        var pointer = input.Pointer;
        if (menu.Contains(ui, pointer)) return false;
        return !(ownerId != 0 && ui.TryGetRect(ownerId, out var owner) && owner.Contains(pointer));
    }
}
