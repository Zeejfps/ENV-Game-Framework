using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Hosting;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const int MenuSalt = 0x4D454E55;
    private const int MenuScrollSalt = 0x4D534352;

    /// <summary>The id of the popup the menu title, dropdown or submenu item with <paramref name="ownerId"/> opens.</summary>
    internal static int MenuPopupId(int ownerId) => HashCode.Combine(ownerId, MenuSalt);

    /// <summary>The id of the item keyed <paramref name="key"/> (its label, or the id it was given) in
    /// the menu whose popup is <paramref name="popupId"/>: <see cref="MenuPopupId"/> of the title,
    /// dropdown or submenu item that opens it, or a context menu's own id.</summary>
    internal static int MenuItemId(int popupId, string key) => HashCode.Combine(popupId, key);

    private static readonly KeyboardKey[] MenuKeySet =
    [
        KeyboardKey.DownArrow, KeyboardKey.UpArrow, KeyboardKey.Home, KeyboardKey.End,
        KeyboardKey.Enter, KeyboardKey.Space, KeyboardKey.RightArrow, KeyboardKey.LeftArrow, KeyboardKey.Tab,
    ];

    /// <summary>One open menu's items and keyboard state. Items are counted as they are recorded;
    /// keys act on last frame's items.</summary>
    internal abstract class MenuPopup
    {
        private readonly record struct Item(int Id, bool Enabled, bool SubMenu);

        private List<Item> _items = new();
        private List<Item> _recording = new();
        private int _owner;
        private bool _childSeen;
        private bool _iconColumn;
        private bool _recordingIconColumn;
        private bool _separatorAsked;
        private PointF _pointer = new(float.NaN, float.NaN);
        private readonly SubMenuAim _aim = new();

        public int PopupId;
        public int Highlight = -1;
        public int HighlightFirstOwner;
        public int PickIndex = -1;
        public int OpenSub = -1;
        public bool SubMenuKeyed;
        public MenuPopup? OpenChild;

        public int ScrollId => HashCode.Combine(PopupId, MenuScrollSalt);

        /// <summary>Whether last frame's items had an icon beside a mark, so every item leaves a
        /// column for icons after the marks and the labels line up.</summary>
        public bool HasIconColumn => _iconColumn;

        public void UseIconColumn() => _recordingIconColumn = true;

        /// <summary>Whether hovering an item takes the highlight this frame: when the user moves the
        /// mouse, not while it rests on an item, and not while it crosses other items on its way to
        /// the open submenu.</summary>
        public bool HoverTakesHighlight { get; private set; }

        public void TrackPointer(Ui ui)
        {
            var pointer = ui.Input.Pointer;
            var moved = pointer != _pointer;
            _pointer = pointer;
            HoverTakesHighlight = moved;
            if (!TryGetOpenSubMenu(ui, out var item, out var subMenu))
            {
                _aim.Stop();
                return;
            }
            if (item.Contains(pointer))
            {
                _aim.Start(pointer, ui.RealTime);
                return;
            }
            if (_aim.IsActive)
                HoverTakesHighlight = !_aim.Holds(ui, pointer, moved, subMenu);
        }

        /// <summary>Where last frame drew the open submenu and the item that opened it.</summary>
        private bool TryGetOpenSubMenu(Ui ui, out UiRect item, out UiRect subMenu)
        {
            subMenu = default;
            if (!ui.TryGetRect(ItemId(OpenSub), out item))
                return false;
            if (OpenChild == null)
                return false;
            return ui.TryGetRect(OpenChild.PopupId, out subMenu);
        }

        public abstract void Close();

        public void BeginItems(int owner)
        {
            if (owner != _owner)
            {
                _owner = owner;
                _items.Clear();
                _iconColumn = false;
                Highlight = -1;
                OpenSub = -1;
                OpenChild = null;
            }
            _recording.Clear();
            _childSeen = false;
            _recordingIconColumn = false;
            _separatorAsked = false;
        }

        /// <summary>Asks for a separator before the next item. One asked for before the first item,
        /// right after another, or with no item after it is left out.</summary>
        public void AskForSeparator()
        {
            if (_recording.Count == 0)
                return;
            _separatorAsked = true;
        }

        /// <summary>Whether a separator was asked for since the last item; taking it clears the ask.</summary>
        public bool TakeSeparator()
        {
            var asked = _separatorAsked;
            _separatorAsked = false;
            return asked;
        }

        public int AddItem(int id, bool enabled, bool subMenu = false)
        {
            _recording.Add(new Item(id, enabled, subMenu));
            return _recording.Count - 1;
        }

        public bool SubMenuAt(int index) => IsItem(index) && _items[index].SubMenu;

        public int ItemId(int index) => IsItem(index) ? _items[index].Id : 0;

        public void ChildSeen(MenuPopup child)
        {
            OpenChild = child;
            _childSeen = true;
        }

        public void EndItems()
        {
            (_items, _recording) = (_recording, _items);
            _iconColumn = _recordingIconColumn;
            _separatorAsked = false;
            if (!_childSeen) OpenChild = null;
            PickIndex = -1;
            if (Highlight >= _items.Count) Highlight = -1;
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

        public bool IsEnabled(int index) => IsItem(index) && _items[index].Enabled;

        private bool IsItem(int index) => index >= 0 && index < _items.Count;

        public int Step(int from, int direction)
        {
            var count = _items.Count;
            if (count == 0) return -1;
            var index = from;
            for (var i = 0; i < count; i++)
            {
                index = index < 0 ? (direction > 0 ? 0 : count - 1) : (index + direction + count) % count;
                if (_items[index].Enabled) return index;
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

    /// <summary>The pointer's way from a submenu's item to the submenu, across the items between.
    /// It holds while the pointer stays in the triangle from where it left the item to the
    /// submenu's near edge, and ends once it leaves the triangle or rests in it for
    /// <see cref="UiTheme.SubMenuAimSeconds"/>.</summary>
    private sealed class SubMenuAim
    {
        private static readonly PointF Nowhere = new(float.NaN, float.NaN);

        private PointF _from = Nowhere;
        private double _movedAt;

        public bool IsActive => !float.IsNaN(_from.X);

        public void Start(PointF from, double now)
        {
            _from = from;
            _movedAt = now;
        }

        public void Stop() => _from = Nowhere;

        /// <summary>Whether the pointer is still on its way to <paramref name="subMenu"/>; once it
        /// is not, the aim stops.</summary>
        public bool Holds(Ui ui, PointF pointer, bool moved, UiRect subMenu)
        {
            if (!IsHeadingTo(subMenu, pointer))
            {
                Stop();
                return false;
            }
            var now = ui.RealTime;
            if (moved)
                _movedAt = now;
            var remaining = ui.Theme.SubMenuAimSeconds - (now - _movedAt);
            if (remaining <= 0)
            {
                Stop();
                return false;
            }
            ui.RequestUpdateIn(remaining);
            return true;
        }

        private bool IsHeadingTo(UiRect subMenu, PointF pointer)
        {
            var edgeX = subMenu.X >= _from.X ? subMenu.X : subMenu.Right;
            var top = new PointF(edgeX, subMenu.Y);
            var bottom = new PointF(edgeX, subMenu.Bottom);
            return IsInTriangle(pointer, _from, top, bottom);
        }

        private static bool IsInTriangle(PointF point, PointF a, PointF b, PointF c)
        {
            var ab = Cross(a, b, point);
            var bc = Cross(b, c, point);
            var ca = Cross(c, a, point);
            var anyNegative = ab < 0 || bc < 0 || ca < 0;
            var anyPositive = ab > 0 || bc > 0 || ca > 0;
            return !(anyNegative && anyPositive);
        }

        private static float Cross(PointF a, PointF b, PointF point) => (b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X);
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

        public void CloseUnlessOpenSeen()
        {
            if (!OpenSeen)
                Close();
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

    /// <summary>A menu opened on demand: a context menu shown <see cref="At"/> a point, or a
    /// dropdown's, built from <see cref="Revision"/>.</summary>
    private sealed class OpenedMenu : MenuPopup
    {
        public bool Open;
        public bool JustOpened;
        public PointF At;
        public int Revision;

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
    /// A press outside or a pick closes it, and keyboard focus returns to where it was. Dispose the
    /// scope once its menus are recorded. Where the window system has a menu bar of its own
    /// (<see cref="Ui.SystemMenus"/>), the menus are recorded into it instead and nothing is drawn.</summary>
    public static Ui.Scope MenuBar(this Ui ui, string id, Box box = default)
    {
        if (ui.SystemMenus is { } system)
        {
            system.BeginBar();
            return ui.Group(system, static menus => ((SystemMenus)menus).EndBar());
        }
        var theme = ui.Theme;
        var barId = ui.GetId(id);
        var state = ui.GetState<MenuBarState>(HashCode.Combine(barId, MenuSalt));
        if (state.Open != 0 && ShouldCloseMenu(ui, barId, state))
            state.Close();
        state.OpenSeen = false;
        state.BeginTitles();

        var menuBar = ui.Group(state, static bar => ((MenuBarState)bar).CloseUnlessOpenSeen());
        ui.Row(barId, box with
        {
            Height = box.Height ?? theme.MenuBarHeight,
            Gap = box.Gap ?? 0f,
            Padding = box.Padding ?? new Spacing(theme.GapTight, theme.GapTight / 2),
            Background = box.Background ?? theme.Surface,
        });
        return menuBar;
    }

    /// <summary>A title in the enclosing <see cref="MenuBar"/> and its dropdown. While
    /// <see cref="Ui.Scope.IsOpen"/>, record its items inside the scope; dispose either way. Ids work
    /// as for <see cref="Button"/>. <paramref name="role"/> tells the window system's menu bar what
    /// it may add to the menu, and is ignored by a bar drawn in the window.</summary>
    public static Ui.Scope Menu(this Ui ui, string label, string? id = null, AppMenuRole role = AppMenuRole.Standard)
    {
        if (RecordingSystemMenus(ui) is { } system)
            return SystemMenu(ui, system, id ?? label, label, role);
        var bar = FindMenuBar(ui);
        var theme = ui.Theme;
        var i = ui.Interact(id ?? label);
        var open = TakeTitle(ui, bar, i);
        var title = TitleBox(ui, i, open, theme.MenuItemPaddingX);
        ui.Text(label, color: open ? theme.SelectionText : theme.Text);
        return OpenTitled(ui, bar, i.Id, open, title);
    }

    /// <summary>A title in the enclosing <see cref="MenuBar"/> showing the window's icon, as title
    /// bars show the menu of the window itself, and its dropdown; otherwise as <see cref="Menu"/>.
    /// Records nothing while the window has no icon.</summary>
    public static Ui.Scope AppIconMenu(this Ui ui, string id)
    {
        if (!ui.HasWindowIcon || RecordingSystemMenus(ui) != null)
            return default;
        var bar = FindMenuBar(ui);
        var theme = ui.Theme;
        var i = ui.Interact(id);
        var open = TakeTitle(ui, bar, i);
        var title = TitleBox(ui, i, open, theme.GapTight);
        ui.AppIcon(theme.IconSize);
        return OpenTitled(ui, bar, i.Id, open, title);
    }

    /// <summary>The window system's menus while a <see cref="MenuBar"/> records into them, and no menu
    /// drawn in the window is open inside it.</summary>
    private static SystemMenus? RecordingSystemMenus(Ui ui)
    {
        if (ui.FindOpenGroup<MenuPopup>() != null)
            return null;
        return ui.FindOpenGroup<SystemMenus>();
    }

    private static Ui.Scope SystemMenu(Ui ui, SystemMenus system, string key, string label, AppMenuRole role)
    {
        system.BeginMenu(key, label, role);
        return ui.Group(system, static menus => ((SystemMenus)menus).EndMenu());
    }

    /// <summary>An item the window system acts on itself, such as macOS's Hide and Quit, recorded into
    /// its menu bar; nothing in a menu drawn in the window, which has no such items.</summary>
    public static void SystemMenuItem(this Ui ui, string label, AppMenuSystemAction action, string? shortcut = null)
    {
        RecordingSystemMenus(ui)?.Item(label, label, shortcut, enabled: true, check: false, action);
    }

    private static MenuBarState FindMenuBar(Ui ui) =>
        ui.FindOpenGroup<MenuBarState>() ?? throw new InvalidOperationException("A Menu must be recorded inside a MenuBar.");

    /// <summary>Adds the title to <paramref name="bar"/> and opens, closes or switches to it as the
    /// pointer asks: a press flips it, and hovering it while another menu is open opens it. Whether
    /// it is open.</summary>
    private static bool TakeTitle(Ui ui, MenuBarState bar, Interaction i)
    {
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
        return bar.Open == i.Id;
    }

    /// <summary>The box a title draws its label or icon in, marked while it is open or hovered.</summary>
    private static Ui.Scope TitleBox(Ui ui, Interaction i, bool open, float paddingX)
    {
        var theme = ui.Theme;
        return ui.Box(i.Id, new Box
        {
            Height = Sizing.Grow(),
            Padding = new Spacing(paddingX, 0f),
            AlignY = Align.Center,
            Radius = theme.CornerRadius,
            Background = TitleBackground(theme, open, i.Hovered),
        });
    }

    private static uint TitleBackground(UiTheme theme, bool open, bool hovered)
    {
        if (open)
            return theme.Selection;
        if (hovered)
            return theme.ControlHovered;
        return 0u;
    }

    /// <summary>The scope the open title's items are recorded in, its dropdown shown below it; the
    /// title closed, and no scope, while it is not open.</summary>
    private static Ui.Scope OpenTitled(Ui ui, MenuBarState bar, int id, bool open, Ui.Scope title)
    {
        if (!open)
        {
            title.Dispose();
            return default;
        }

        bar.OpenSeen = true;
        bar.PopupId = MenuPopupId(id);
        bar.BeginItems(id);
        ui.TryGetRect(id, out var anchor);
        OpenMenu(ui, bar, anchor);
        if (MenuKeys(ui, bar, isSubMenu: false) is var direction and not 0)
        {
            bar.Open = bar.Neighbor(id, direction);
            bar.HighlightFirstOwner = bar.Open;
        }
        return title;
    }

    /// <summary>Opens the <see cref="ContextMenu(Ui, string)"/> with <paramref name="id"/>, in the same
    /// id scope, at <paramref name="at"/> or else the pointer.</summary>
    public static void OpenContextMenu(this Ui ui, string id, PointF? at = null) => ui.OpenContextMenu(ui.GetId(id), at);

    /// <summary>Opens the <see cref="ContextMenu(Ui, int)"/> with <paramref name="id"/> at
    /// <paramref name="at"/> or else the pointer. Take the id once with <see cref="Ui.GetId(string)"/>
    /// and pass it to both, and they match wherever each is recorded.</summary>
    public static void OpenContextMenu(this Ui ui, int id, PointF? at = null)
    {
        var state = ui.GetState<OpenedMenu>(HashCode.Combine(id, MenuSalt));
        state.Open = true;
        state.JustOpened = true;
        state.At = at ?? ui.Input.Pointer;
    }

    public static Ui.Scope ContextMenu(this Ui ui, string id) => ui.ContextMenu(ui.GetId(id));

    /// <summary>A menu shown where <see cref="OpenContextMenu(Ui, int, PointF?)"/> asked, until Escape,
    /// a press outside, or a pick closes it. Record it every frame. Keys work as in a
    /// <see cref="MenuBar"/>, and the scope as a <see cref="Menu"/>'s.</summary>
    public static Ui.Scope ContextMenu(this Ui ui, int id)
    {
        var state = ui.GetState<OpenedMenu>(HashCode.Combine(id, MenuSalt));
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
    /// Right or Enter; Left or Escape closes just that menu. The scope is as a <see cref="Menu"/>'s;
    /// ids work as for <see cref="Button"/>. In the window system's menu bar it is always enabled,
    /// and <paramref name="role"/> tells the bar what it may add to it.</summary>
    public static Ui.Scope SubMenu(this Ui ui, string label, bool enabled = true, string? id = null, AppMenuRole role = AppMenuRole.Standard)
    {
        if (RecordingSystemMenus(ui) is { } system)
            return SystemMenu(ui, system, id ?? label, label, role);
        var parent = ui.FindOpenGroup<MenuPopup>() ?? throw new InvalidOperationException("A SubMenu must be recorded inside a menu.");
        DrawAskedSeparator(ui, parent);
        var theme = ui.Theme;
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        var index = parent.AddItem(i.Id, enabled, subMenu: true);
        if (enabled && ((parent.HoverTakesHighlight && ui.IsPointerOver(i.Id)) || i.Clicked))
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
            if (parent.HasIconColumn)
                ui.Leaf(new Box { Width = theme.MenuIconColumnWidth, Height = 0f });
            ui.Text(label, color: color);
            ui.Leaf(new Box { Width = Sizing.Grow(theme.MenuShortcutGap), Height = 0f });
            using (ui.Box(new Box { Width = theme.IconSize, Height = Sizing.Grow() }))
                ui.PaintGlyph(UiGlyph.DisclosureClosed, color);
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
    /// <paramref name="check"/> shows a check mark when true and leaves room for one when false,
    /// with <paramref name="icon"/>, when given, between it and the label. Without a check the
    /// icon sits where the mark would, unless another item of the menu has an icon beside its mark,
    /// when every icon and label lines up after the marks. <paramref name="shortcut"/> is only
    /// displayed. Ids work as for <see cref="Button"/>.</summary>
    public static bool MenuItem(this Ui ui, string label, string? shortcut = null, bool enabled = true, bool? check = null, string? id = null,
        UiIcon? icon = null)
    {
        var mark = check == true ? MenuMark.Check : MenuMark.None;
        return ui.MenuItem(label, shortcut, enabled, mark, check != null, id, icon, keepOpen: false);
    }

    /// <summary>One choice of a set: shows a dot when <paramref name="selected"/> and leaves room
    /// for one otherwise, with <paramref name="icon"/>, when given, between it and the label.
    /// Returns true on the frame it is picked, like <see cref="MenuItem(Ui, string, string?, bool, bool?, string?, UiIcon?)"/>.</summary>
    public static bool MenuRadioItem(this Ui ui, string label, bool selected, string? shortcut = null, bool enabled = true, string? id = null,
        UiIcon? icon = null)
    {
        var mark = selected ? MenuMark.Dot : MenuMark.None;
        return ui.MenuItem(label, shortcut, enabled, mark, markable: true, id, icon, keepOpen: false);
    }

    private enum MenuMark
    {
        None,
        Check,
        Dot,
    }

    /// <summary>An item of a menu: a <paramref name="markable"/> one keeps its mark column for the
    /// mark and shows <paramref name="icon"/> in the icon column after it.</summary>
    private static bool MenuItem(this Ui ui, string label, string? shortcut, bool enabled, MenuMark mark, bool markable, string? id, UiIcon? icon,
        bool keepOpen)
    {
        if (RecordingSystemMenus(ui) is { } system)
            return system.Item(id ?? label, label, shortcut, enabled, mark != MenuMark.None);
        var theme = ui.Theme;
        var menu = ui.FindOpenGroup<MenuPopup>();
        DrawAskedSeparator(ui, menu);
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        var index = menu?.AddItem(i.Id, enabled) ?? -1;
        var over = enabled && ui.IsPointerOver(i.Id);
        if (over && menu is { HoverTakesHighlight: true })
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
        var besideMark = markable && icon != null;
        if (besideMark)
            menu?.UseIconColumn();
        var menuHasIconColumn = menu != null && menu.HasIconColumn;
        var iconColumn = besideMark || menuHasIconColumn;

        using (ui.Row(i.Id, MenuRowBox(theme, highlighted)))
        {
            if (iconColumn)
            {
                MarkColumn(ui, mark, null, color);
                IconColumn(ui, icon);
            }
            else
            {
                MarkColumn(ui, mark, icon, color);
            }
            ui.Text(label, color: color);
            ui.Leaf(new Box { Width = Sizing.Grow(theme.MenuShortcutGap), Height = 0f });
            if (shortcut != null)
                ui.Text(shortcut, color: enabled ? theme.TextMuted : theme.TextDisabled);
        }

        if (picked && !keepOpen)
            menu?.Close();
        return picked;
    }

    /// <summary>The column of an item's mark, or of its <paramref name="icon"/> when it has no mark
    /// and the menu no icon column.</summary>
    private static void MarkColumn(Ui ui, MenuMark mark, UiIcon? icon, uint color)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = theme.MenuIconColumnWidth, Height = Sizing.Grow(), AlignX = Align.Center, AlignY = Align.Center }))
        {
            if (icon != null)
            {
                ui.Icon(icon.Value);
                return;
            }
            using (ui.Box(new Box { Width = theme.IconSize, Height = theme.IconSize }))
            {
                if (mark == MenuMark.Check)
                    ui.PaintGlyph(UiGlyph.Checkmark, color);
                else if (mark == MenuMark.Dot)
                    ui.Paint(color, static (p, c) => p.Circle(new PointF(p.Rect.X + p.Rect.Width / 2, p.Rect.Y + p.Rect.Height / 2), p.Rect.Height * 0.2f, c));
            }
        }
    }

    private static void IconColumn(Ui ui, UiIcon? icon)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = theme.MenuIconColumnWidth, Height = Sizing.Grow(), AlignY = Align.Center }))
        {
            if (icon != null)
                ui.Icon(icon.Value);
        }
    }

    /// <summary>A checkable item: picking it flips <paramref name="value"/>. With
    /// <paramref name="keepOpen"/> the menu stays open, so several items can be flipped in a row.
    /// An <paramref name="icon"/> is drawn between the check and the label.</summary>
    public static bool MenuItem(this Ui ui, string label, ref bool value, string? shortcut = null, bool enabled = true, string? id = null,
        bool keepOpen = false, UiIcon? icon = null)
    {
        var mark = value ? MenuMark.Check : MenuMark.None;
        var picked = ui.MenuItem(label, shortcut, enabled, mark, markable: true, id, icon, keepOpen);
        if (picked) value = !value;
        return picked;
    }

    /// <summary>A rule between the items before it and those after; drawn only once an item follows,
    /// so a menu never starts or ends with one, nor draws two in a row.</summary>
    public static void MenuSeparator(this Ui ui)
    {
        if (RecordingSystemMenus(ui) is { } system)
        {
            system.Separator();
            return;
        }
        var menu = ui.FindOpenGroup<MenuPopup>();
        if (menu == null)
        {
            DrawSeparator(ui);
            return;
        }
        menu.AskForSeparator();
    }

    /// <summary>The separator asked for before this item of <paramref name="menu"/>, when one was.</summary>
    private static void DrawAskedSeparator(Ui ui, MenuPopup? menu)
    {
        if (menu == null)
            return;
        if (!menu.TakeSeparator())
            return;
        DrawSeparator(ui);
    }

    private static void DrawSeparator(Ui ui)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = Sizing.Grow(), Padding = new Spacing(theme.MenuItemPaddingX, theme.GapTight) }))
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = theme.SeparatorWidth, Background = theme.Separator });
    }

    /// <summary>Records the popup of <paramref name="menu"/> at <paramref name="anchor"/>: an input
    /// layer holding a column of items that scrolls once taller than <see cref="UiTheme.MenuMaxHeight"/>.</summary>
    private static Ui.Scope OpenMenu(Ui ui, MenuPopup menu, UiRect anchor, float gap = 0f, PopupSide side = PopupSide.Below)
    {
        var theme = ui.Theme;
        menu.TrackPointer(ui);
        var scope = ui.Group(menu, static items => ((MenuPopup)items).EndItems());
        ui.Layer(menu.PopupId);
        ui.Popup(menu.PopupId, anchor, MenuBox(theme), gap: gap, side: side);
        ui.Scroll(menu.ScrollId, new Box { Width = Sizing.Grow(), Height = Sizing.Fit(max: theme.MenuMaxHeight), Gap = 0f });
        // Items are keyed by their labels, which often come from the app's data, so they get an id
        // scope of the menu's own: a label that matches an id outside the menu, such as an open
        // dialog's, would otherwise be taken for it. See MenuItemId. Closed with the rest when the
        // menu's scope is.
        ui.UniqueId(menu.PopupId);
        return scope;
    }

    private static Box MenuBox(UiTheme theme) => theme.PopupSurface(new Box
    {
        Width = Sizing.Fit(theme.MenuMinWidth),
        Padding = theme.MenuPadding,
    });

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
