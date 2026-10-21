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
            _state.Open = 0;
    }
}

/// <summary>A menu from <see cref="UiWidgets.Menu"/> or <see cref="UiWidgets.ContextMenu"/>.
/// While <see cref="IsOpen"/>, record its items inside the scope; dispose either way.</summary>
public readonly struct MenuScope : IDisposable
{
    private readonly Ui.Scope _popup;
    private readonly Ui.Scope _provided;
    private readonly Ui.Scope _title;
    private readonly bool _hasTitle;

    internal MenuScope(bool isOpen, Ui.Scope popup, Ui.Scope provided, Ui.Scope title, bool hasTitle)
    {
        IsOpen = isOpen;
        _popup = popup;
        _provided = provided;
        _title = title;
        _hasTitle = hasTitle;
    }

    public bool IsOpen { get; }

    public void Dispose()
    {
        if (IsOpen)
        {
            _provided.Dispose();
            _popup.Dispose();
        }
        if (_hasTitle)
            _title.Dispose();
    }
}

public static partial class UiWidgets
{
    private const int MenuSalt = 0x4D454E55;
    private const float MenuMinWidth = 160f;
    private const float MenuCheckWidth = 22f;
    private const float MenuShortcutGap = 32f;

    internal abstract class MenuPopup
    {
        public abstract void Close();
    }

    internal sealed class MenuBarState : MenuPopup
    {
        public int Open;
        public int OpenPopup;
        public bool OpenSeen;

        public override void Close() => Open = 0;
    }

    private sealed class ContextMenuState : MenuPopup
    {
        public bool Open;
        public bool JustOpened;
        public PointF At;

        public override void Close() => Open = false;
    }

    /// <summary>A row of menu titles; each <see cref="Menu"/> recorded inside opens a dropdown.
    /// Once one is open, hovering another title switches to it. Escape, a press outside, or a
    /// pick closes it.</summary>
    public static MenuBar MenuBar(this Ui ui, string id, Box box = default)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var barId = ui.GetId(id);
        var state = ui.GetState<MenuBarState>(HashCode.Combine(barId, MenuSalt));
        if (state.Open != 0 && ShouldCloseMenu(ui, barId, state.OpenPopup))
            state.Open = 0;
        state.OpenSeen = false;

        var row = ui.Row(barId, box with
        {
            Height = box.Height ?? theme.ControlHeight,
            Gap = box.Gap ?? 0f,
            Padding = box.Padding ?? new Spacing(4f, 2f),
            Background = box.Background != 0 ? box.Background : style.Surface,
        });
        return new MenuBar(row, ui.Provide(state), state);
    }

    /// <summary>A title in the enclosing <see cref="MenuBar"/> and its dropdown. Ids work as for
    /// <see cref="Button"/>.</summary>
    public static MenuScope Menu(this Ui ui, string label, string? id = null)
    {
        var bar = ui.Get<MenuBarState>() ?? throw new InvalidOperationException("A Menu must be recorded inside a MenuBar.");
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var i = ui.Interact(id ?? label);
        if (i.Pressed)
            bar.Open = bar.Open == i.Id ? 0 : i.Id;
        else if (bar.Open != 0 && bar.Open != i.Id && i.Hovered)
            bar.Open = i.Id;
        var open = bar.Open == i.Id;

        var title = ui.Box(i.Id, new Box
        {
            Height = Sizing.Grow(),
            Padding = new Spacing(10f, 0f),
            AlignY = Align.Center,
            Radius = 4f,
            Background = open ? style.Pressed(style.Surface) : i.Hovered ? style.Hovered(style.Surface) : 0u,
        });
        ui.Text(id != null ? label : Visible(ui, i.Id, label));
        if (!open)
            return new MenuScope(false, default, default, title, true);

        bar.OpenSeen = true;
        bar.OpenPopup = HashCode.Combine(i.Id, MenuSalt);
        ui.TryGetRect(i.Id, out var anchor);
        var popup = ui.Popup(bar.OpenPopup, anchor, MenuBox(ui, theme));
        return new MenuScope(true, popup, ui.Provide<MenuPopup>(bar), title, true);
    }

    /// <summary>Opens the <see cref="ContextMenu"/> with <paramref name="id"/>, in the same id
    /// scope, at <paramref name="at"/> or else the pointer.</summary>
    public static void OpenContextMenu(this Ui ui, string id, PointF? at = null)
    {
        var state = ui.GetState<ContextMenuState>(HashCode.Combine(ui.GetId(id), MenuSalt));
        state.Open = true;
        state.JustOpened = true;
        state.At = at ?? ui.Input.Pointer;
    }

    /// <summary>A menu shown where <see cref="OpenContextMenu"/> asked, until Escape, a press
    /// outside, or a pick closes it. Record it every frame.</summary>
    public static MenuScope ContextMenu(this Ui ui, string id)
    {
        var menuId = ui.GetId(id);
        var state = ui.GetState<ContextMenuState>(HashCode.Combine(menuId, MenuSalt));
        if (state.Open && !state.JustOpened && ShouldCloseMenu(ui, 0, menuId))
            state.Open = false;
        state.JustOpened = false;
        if (!state.Open)
            return default;

        var popup = ui.Popup(menuId, new UiRect(state.At.X, state.At.Y, 0f, 0f), MenuBox(ui, WidgetTheme.For(ui)));
        return new MenuScope(true, popup, ui.Provide<MenuPopup>(state), default, false);
    }

    /// <summary>Returns true on the frame the item is picked, which also closes its menu.
    /// <paramref name="check"/> shows a check mark when true and leaves room for one when false;
    /// <paramref name="shortcut"/> is only displayed. Ids work as for <see cref="Button"/>.</summary>
    public static bool MenuItem(this Ui ui, string label, string? shortcut = null, bool enabled = true, bool? check = null, string? id = null)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        var highlighted = enabled && (i.Hovered || i.Held);
        var color = highlighted ? style.OnAccent : enabled ? style.Text : style.TextMuted;

        using (ui.Row(i.Id, new Box
        {
            Width = Sizing.Grow(),
            Height = theme.MenuItemHeight,
            Gap = 0f,
            Padding = new Spacing(2f, 0f, 10f, 0f),
            Radius = 4f,
            Background = highlighted ? style.Accent : 0u,
        }))
        {
            using (ui.Box(new Box { Width = MenuCheckWidth, Height = Sizing.Grow(), Padding = new Spacing(5f, 6f) }))
            {
                if (check == true)
                    ui.Paint(color, static (p, c) => DrawCheckmark(p, c));
            }
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: color);
            ui.Leaf(new Box { Width = Sizing.Grow(MenuShortcutGap), Height = 0f });
            if (shortcut != null)
                ui.Text(shortcut, color: highlighted ? color : style.TextMuted);
        }

        if (i.Clicked)
            ui.Get<MenuPopup>()?.Close();
        return i.Clicked;
    }

    /// <summary>A checkable item: picking it flips <paramref name="value"/>.</summary>
    public static bool MenuItem(this Ui ui, string label, ref bool value, string? shortcut = null, bool enabled = true, string? id = null)
    {
        var picked = ui.MenuItem(label, shortcut, enabled, value, id);
        if (picked) value = !value;
        return picked;
    }

    public static void MenuSeparator(this Ui ui)
    {
        using (ui.Box(new Box { Width = Sizing.Grow(), Padding = new Spacing(6f, 4f) }))
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 1f, Background = ui.Style.Border });
    }

    private static Box MenuBox(Ui ui, WidgetTheme theme)
    {
        var style = ui.Style;
        return new Box
        {
            Width = Sizing.Fit(MenuMinWidth),
            Padding = 4f,
            Background = style.Surface,
            BorderColor = style.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        };
    }

    private static bool ShouldCloseMenu(Ui ui, int ownerId, int popupId)
    {
        var input = ui.Input;
        if (input.PopupsDismissed || input.IsKeyPressed(KeyboardKey.Escape)) return true;
        if (!input.WasPressed(PointerButton.Left) && !input.WasPressed(PointerButton.Right) && !input.WasPressed(PointerButton.Middle))
            return false;
        var pointer = input.Pointer;
        if (ui.TryGetRect(popupId, out var popup) && popup.Contains(pointer)) return false;
        return !(ownerId != 0 && ui.TryGetRect(ownerId, out var owner) && owner.Contains(pointer));
    }
}
