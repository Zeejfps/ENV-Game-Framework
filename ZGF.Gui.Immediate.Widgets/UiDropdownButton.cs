using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A <see cref="UiWidgets.SplitIconButton"/>: act on <see cref="Clicked"/>, and while
/// <see cref="IsOpen"/>, record its menu's items inside the scope; dispose either way.</summary>
public readonly struct SplitButtonScope : IDisposable
{
    private readonly Ui.Scope _menu;

    internal SplitButtonScope(bool clicked, Ui.Scope menu)
    {
        Clicked = clicked;
        _menu = menu;
    }

    /// <summary>True on the frame the button's main half is clicked, or activated with Enter or Space.</summary>
    public bool Clicked { get; }

    public bool IsOpen => _menu.IsOpen;

    public void Dispose() => _menu.Dispose();
}

public static partial class UiWidgets
{
    private const int SplitArrowSalt = 0x53504C41;
    private const int SplitShellSalt = 0x53504C53;

    internal static int SplitArrowId(int buttonId) => HashCode.Combine(buttonId, SplitArrowSalt);

    internal static int SplitShellId(int buttonId) => HashCode.Combine(buttonId, SplitShellSalt);

    internal static int SplitMenuPopupId(int buttonId) => MenuPopupId(SplitArrowId(buttonId));

    /// <summary>A narrow <see cref="ButtonVariant.Ghost"/> button showing a down triangle that opens a
    /// menu below it. Pressing it again, a pick, Escape, Tab or a press outside closes the menu, as
    /// does a change of <paramref name="revision"/> — pass what the items are built from, so a pick
    /// never lands on a row that moved. Enter or Space while focused opens it with its first item
    /// highlighted, and keys then work as in a <see cref="MenuBar"/>. While
    /// <see cref="Ui.Scope.IsOpen"/>, record its items inside the scope; dispose either way.</summary>
    public static Ui.Scope DropdownButton(this Ui ui, string id, string? tooltip = null, bool enabled = true, int revision = 0) =>
        ui.DropdownButton(ui.GetId(id), tooltip, enabled, revision);

    public static Ui.Scope DropdownButton(this Ui ui, int id, string? tooltip = null, bool enabled = true, int revision = 0)
    {
        var theme = ui.Theme;
        var i = Pressable(ui, id, enabled, out var activated, out var ring);
        var state = DropdownMenuState(ui, i.Id, enabled, revision);
        if (i.Pressed || activated)
            ToggleDropdown(state, revision, activated);
        var open = state.Open;
        var (fill, _, color) = ButtonColors(theme, ButtonVariant.Ghost, enabled, i.Held || open, i.Hovered);

        using (ui.Box(i.Id, new Box
        {
            Width = theme.DropdownButtonWidth,
            Height = theme.IconButtonSize,
            Background = fill,
            Radius = theme.CornerRadius,
        }))
        {
            ui.PaintGlyph(UiGlyph.DropdownArrow, color);
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        if (tooltip != null && !open)
            ui.Tooltip(i.Id, tooltip);
        ui.TryGetRect(i.Id, out var anchor);
        return DropdownMenu(ui, state, anchor);
    }

    /// <summary>An icon button joined to a dropdown arrow on its right, drawn as one outlined control
    /// with a divider between the halves, like a <see cref="SegmentedControl{T}"/>; each half lights
    /// when hovered and darkens when pressed, the arrow also while its menu is open. The
    /// menu opens below the whole control and closes as a dropdown's does; <paramref name="revision"/>
    /// is as there. The control is one Tab stop: Enter or Space clicks it, Down or Alt+Down opens its
    /// menu. <paramref name="tooltip"/> is for the main half, <paramref name="menuTooltip"/> for the arrow.</summary>
    public static SplitButtonScope SplitIconButton(this Ui ui, string id, IconGlyph glyph, string? tooltip = null,
        string? menuTooltip = null, bool enabled = true, int revision = 0)
    {
        var theme = ui.Theme;
        var main = Pressable(ui, id, enabled, out var activated, out var ring);
        var arrowId = SplitArrowId(main.Id);
        var shellId = SplitShellId(main.Id);
        var arrow = enabled ? ui.Clickable(arrowId) : Interaction.Inert(arrowId);
        var keyed = enabled && ui.HasFocus(main.Id) && ui.Input.TakeKey(KeyboardKey.DownArrow);
        var state = DropdownMenuState(ui, arrowId, enabled, revision);
        if (arrow.Pressed || keyed)
            ToggleDropdown(state, revision, keyed);
        var open = state.Open;

        var inner = JoinedInnerRadius(theme);
        var mainFill = ButtonColors(theme, ButtonVariant.Ghost, enabled, main.Held || activated, main.Hovered).Fill;
        var arrowFill = ButtonColors(theme, ButtonVariant.Ghost, enabled, arrow.Held || open, arrow.Hovered).Fill;
        var color = enabled ? theme.Text : theme.TextDisabled;

        using (JoinedRow(ui, shellId, theme.IconButtonSize))
        {
            if (ring) FocusRing(ui, theme.CornerRadius);
            using (ui.Box(main.Id, new Box
            {
                Width = theme.IconButtonSize,
                Height = Sizing.Grow(),
                AlignX = Align.Center,
                AlignY = Align.Center,
            }))
            {
                PaintJoinedFill(ui, mainFill, Corners.Left(inner));
                ui.Glyph(glyph, color, theme.IconSize, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize });
            }
            ui.Leaf(new Box { Width = theme.BorderWidth, Height = Sizing.Grow(), Background = theme.Border });
            using (ui.Box(arrowId, new Box { Width = theme.DropdownButtonWidth, Height = Sizing.Grow() }))
            {
                PaintJoinedFill(ui, arrowFill, Corners.Right(inner));
                ui.PaintGlyph(UiGlyph.DropdownArrow, color);
            }
        }
        if (tooltip != null)
            ui.Tooltip(main.Id, tooltip);
        if (menuTooltip != null && !open)
            ui.Tooltip(arrowId, menuTooltip);
        ui.TryGetRect(shellId, out var anchor);
        return new SplitButtonScope(main.Clicked || activated, DropdownMenu(ui, state, anchor));
    }

    /// <summary>A button showing <paramref name="value"/>'s label and a down triangle that opens a menu
    /// of <paramref name="options"/> below it, the current one marked; picking one sets
    /// <paramref name="value"/> and returns true on that frame.</summary>
    public static bool Select<T>(this Ui ui, string id, ref T value, ReadOnlySpan<T> options, Func<T, string> label,
        Sizing? width = null, string? tooltip = null, bool enabled = true)
    {
        var changed = false;
        using var menu = ui.SelectButton(id, label(value), width, tooltip, enabled, options.Length);
        if (!menu.IsOpen) return false;
        foreach (var option in options)
        {
            var selected = EqualityComparer<T>.Default.Equals(option, value);
            if (!ui.MenuRadioItem(label(option), selected) || selected) continue;
            value = option;
            changed = true;
        }
        return changed;
    }

    /// <summary>How wide a <see cref="SelectButton"/> must be to show the widest of
    /// <paramref name="labels"/> whole, so it keeps one width whichever it shows.</summary>
    public static float SelectButtonWidth(this Ui ui, ReadOnlySpan<string> labels)
    {
        var theme = ui.Theme;
        var widest = 0f;
        foreach (var label in labels)
            widest = MathF.Max(widest, ui.MeasureText(label));
        return MathF.Ceiling(widest) + theme.ControlPaddingX * 2 + theme.GapTight + theme.DropdownArrowWidth + theme.BorderWidth * 2;
    }

    /// <summary>A button showing <paramref name="text"/> and a down triangle that opens a menu below
    /// it, for picking what the text names: a <see cref="Select{T}"/>, or several checked items kept
    /// open with <c>MenuItem(label, ref value, keepOpen: true)</c>. The menu closes and is keyed as a
    /// <see cref="DropdownButton"/>'s, <paramref name="revision"/> included. While
    /// <see cref="Ui.Scope.IsOpen"/>, record its items inside the scope; dispose either way.</summary>
    public static Ui.Scope SelectButton(this Ui ui, string id, string text, Sizing? width = null, string? tooltip = null,
        bool enabled = true, int revision = 0)
    {
        var theme = ui.Theme;
        var i = Pressable(ui, id, enabled, out var activated, out var ring);
        var state = DropdownMenuState(ui, i.Id, enabled, revision);
        if (i.Pressed || activated)
            ToggleDropdown(state, revision, activated);
        var open = state.Open;
        var (fill, border, color) = ButtonColors(theme, ButtonVariant.Neutral, enabled, i.Held || open, i.Hovered);

        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Gap = theme.GapTight,
            AlignY = Align.Center,
            Background = fill,
            BorderColor = border,
            BorderWidth = border != 0 ? theme.BorderWidth : 0f,
            Radius = theme.CornerRadius,
        }))
        {
            ui.Text(text, new Box { Width = Sizing.Grow() }, color: color, overflow: TextOverflow.Ellipsis);
            using (ui.Box(new Box { Width = theme.DropdownArrowWidth, Height = Sizing.Grow() }))
                ui.PaintGlyph(UiGlyph.DropdownArrow, color);
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        if (tooltip != null && !open)
            ui.Tooltip(i.Id, tooltip);
        ui.TryGetRect(i.Id, out var anchor);
        return DropdownMenu(ui, state, anchor);
    }

    /// <summary>Opens the outlined row that joins controls into one, as a
    /// <see cref="SegmentedControl{T}"/>'s segments or a <see cref="SplitIconButton"/>'s halves.</summary>
    private static Ui.Scope JoinedRow(Ui ui, int id, float height)
    {
        var theme = ui.Theme;
        return ui.Row(id, new Box
        {
            Width = Sizing.Fit(),
            Height = height,
            Gap = 0f,
            Padding = theme.BorderWidth,
            Background = theme.Control,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        });
    }

    private static float JoinedInnerRadius(UiTheme theme) => MathF.Max(0f, theme.CornerRadius - theme.BorderWidth);

    /// <summary>Fills the part of a <see cref="JoinedRow"/> being recorded, rounded at the row's ends.</summary>
    private static void PaintJoinedFill(Ui ui, uint fill, Corners corners)
    {
        if (fill != 0)
            ui.Paint((fill, corners), static (p, s) => p.FillRect(p.Rect, s.fill, s.corners));
    }

    private static OpenedMenu DropdownMenuState(Ui ui, int ownerId, bool enabled, int revision)
    {
        var popupId = MenuPopupId(ownerId);
        var state = ui.GetState<OpenedMenu>(popupId);
        state.PopupId = popupId;
        if (state.Open && (!enabled || revision != state.Revision || ShouldCloseMenu(ui, ownerId, state)))
            state.Close();
        return state;
    }

    private static void ToggleDropdown(OpenedMenu state, int revision, bool keyed)
    {
        if (state.Open)
        {
            state.Close();
            return;
        }
        state.Open = true;
        state.Revision = revision;
        if (keyed)
            state.HighlightFirstOwner = state.PopupId;
    }

    private static Ui.Scope DropdownMenu(Ui ui, OpenedMenu state, UiRect anchor)
    {
        if (!state.Open)
            return default;
        state.BeginItems(state.PopupId);
        var menu = OpenMenu(ui, state, anchor);
        MenuKeys(ui, state, isSubMenu: false);
        return menu;
    }
}
