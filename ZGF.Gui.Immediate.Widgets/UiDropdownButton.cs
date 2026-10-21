using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>A <see cref="UiWidgets.SplitIconButton"/>: act on <see cref="Clicked"/>, and while
/// <see cref="IsOpen"/>, record its menu's items inside the scope; dispose either way.</summary>
public readonly struct SplitButtonScope : IDisposable
{
    private readonly MenuScope _menu;

    internal SplitButtonScope(bool clicked, MenuScope menu)
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

    private sealed class DropdownState : MenuPopup
    {
        public bool Open;
        public int Revision;

        public override void Close()
        {
            Open = false;
            Reset();
        }
    }

    /// <summary>A narrow <see cref="ButtonVariant.Ghost"/> button showing a down triangle that opens a
    /// menu below it. Pressing it again, a pick, Escape, Tab or a press outside closes the menu, as
    /// does a change of <paramref name="revision"/> — pass what the items are built from, so a pick
    /// never lands on a row that moved. Enter or Space while focused opens it with its first item
    /// highlighted, and keys then work as in a <see cref="MenuBar"/>. While
    /// <see cref="MenuScope.IsOpen"/>, record its items inside the scope; dispose either way.</summary>
    public static MenuScope DropdownButton(this Ui ui, string id, string? tooltip = null, bool enabled = true, int revision = 0)
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
            ui.Paint((color, theme.DropdownArrowWidth), static (p, s) => DrawDropdownArrow(p, s.color, s.DropdownArrowWidth));
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        if (tooltip != null && !open)
            ui.Tooltip(i.Id, tooltip);
        ui.TryGetRect(i.Id, out var anchor);
        return DropdownMenu(ui, state, anchor);
    }

    /// <summary>An <see cref="IconButton"/> joined to a <see cref="DropdownButton"/> on its right, drawn
    /// as one control: hovering either half lights the whole, and the half pressed is darker. The
    /// menu opens below the whole control and closes as a dropdown's does; <paramref name="revision"/>
    /// is as there. The control is one Tab stop: Enter or Space clicks it, Down or Alt+Down opens its
    /// menu. <paramref name="tooltip"/> is for the main half, <paramref name="menuTooltip"/> for the arrow.</summary>
    public static SplitButtonScope SplitIconButton(this Ui ui, string id, string glyph, string? family = null, string? tooltip = null,
        string? menuTooltip = null, bool enabled = true, int revision = 0)
    {
        var theme = ui.Theme;
        var main = Pressable(ui, id, enabled, out var activated, out var ring);
        var arrowId = HashCode.Combine(main.Id, SplitArrowSalt);
        var shellId = HashCode.Combine(main.Id, SplitShellSalt);
        var arrow = enabled ? ui.Interact(arrowId) : Interaction.Inert(arrowId);
        if (arrow.Hovered) ui.SetCursor(MouseCursor.Hand);
        var keyed = enabled && ui.HasFocus(main.Id) && ui.Input.TakeKey(KeyboardKey.DownArrow);
        var state = DropdownMenuState(ui, arrowId, enabled, revision);
        if (arrow.Pressed || keyed)
            ToggleDropdown(state, revision, keyed);
        var open = state.Open;

        var radius = theme.CornerRadius;
        var lit = enabled && (main.Hovered || arrow.Hovered || open);
        var mainFill = enabled && (main.Held || activated) ? theme.ControlPressed : 0u;
        var arrowFill = enabled && (arrow.Held || open) ? theme.ControlPressed : 0u;
        var color = enabled ? theme.Text : theme.TextDisabled;
        var divider = lit ? theme.Border : 0u;

        using (ui.Row(shellId, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.IconButtonSize,
            Gap = 0f,
            Radius = radius,
            Background = lit ? theme.ControlHovered : 0u,
        }))
        {
            using (ui.Box(main.Id, new Box
            {
                Width = theme.IconButtonSize,
                Height = Sizing.Grow(),
                AlignX = Align.Center,
                AlignY = Align.Center,
            }))
            {
                if (mainFill != 0)
                    ui.Paint((mainFill, radius), static (p, s) => p.FillRect(p.Rect, s.mainFill, Corners.Left(s.radius)));
                ui.Text(glyph, new Box { Width = Sizing.Fit(theme.IconSize), Height = theme.IconSize }, color: color, size: theme.IconSize,
                    align: Align.Center, family: family);
            }
            using (ui.Box(arrowId, new Box { Width = theme.DropdownButtonWidth, Height = Sizing.Grow() }))
            {
                ui.Paint((arrowFill, radius, divider, theme.BorderWidth, color, theme.DropdownArrowWidth), static (p, s) =>
                {
                    var r = p.Rect;
                    if (s.arrowFill != 0)
                        p.FillRect(r, s.arrowFill, Corners.Right(s.radius));
                    if (s.divider != 0)
                        p.FillRect(new UiRect(r.X, r.Y + r.Height / 4, s.BorderWidth, r.Height / 2), s.divider);
                    DrawDropdownArrow(p, s.color, s.DropdownArrowWidth);
                });
            }
            if (ring) FocusRing(ui, radius);
        }
        if (tooltip != null)
            ui.Tooltip(main.Id, tooltip);
        if (menuTooltip != null && !open)
            ui.Tooltip(arrowId, menuTooltip);
        ui.TryGetRect(shellId, out var anchor);
        return new SplitButtonScope(main.Clicked || activated, DropdownMenu(ui, state, anchor));
    }

    private static DropdownState DropdownMenuState(Ui ui, int ownerId, bool enabled, int revision)
    {
        var state = ui.GetState<DropdownState>(HashCode.Combine(ownerId, MenuSalt));
        state.PopupId = HashCode.Combine(ownerId, MenuSalt);
        if (state.Open && (!enabled || revision != state.Revision || ShouldCloseMenu(ui, ownerId, state)))
            state.Close();
        return state;
    }

    private static void ToggleDropdown(DropdownState state, int revision, bool keyed)
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

    private static MenuScope DropdownMenu(Ui ui, DropdownState state, UiRect anchor)
    {
        if (!state.Open)
            return default;
        state.BeginItems(state.PopupId);
        var menu = OpenMenu(ui, state, anchor);
        MenuKeys(ui, state, isSubMenu: false);
        return menu;
    }

    private static void DrawDropdownArrow(UiPainter p, uint color, float width)
    {
        var r = p.Rect;
        var half = width / 2;
        var top = MathF.Round(r.Y + (r.Height - half) / 2);
        var cx = r.X + r.Width / 2;
        var apex = new PointF(cx, top + half);
        for (var x = -half; x <= half; x += 0.5f)
            p.Line(new PointF(cx + x, top), apex, 1f, color);
    }
}
