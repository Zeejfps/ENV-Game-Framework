namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const int TooltipId = 0x544F4F4C;

    private sealed class TooltipState
    {
        public int Target;
        public int Owner;
        public double Since;
        public bool Suppressed;
    }

    public static void Tooltip(this Ui ui, Interaction target, string text) => ui.Tooltip(target.Id, text);

    /// <summary>Shows <paramref name="text"/> under the element with <paramref name="id"/> once the
    /// pointer has rested on it for the theme's tooltip delay, until it leaves or a button is
    /// pressed. Long text wraps into a block; <c>\n</c> breaks lines. Call it every frame the
    /// element is recorded.</summary>
    public static void Tooltip(this Ui ui, int id, string text)
    {
        using var tooltip = ui.TooltipContent(id);
        if (tooltip.IsOpen)
            ui.Text(text, color: ui.Theme.Text, size: ui.Theme.FontSizeSmall, wrap: true);
    }

    /// <summary>A tooltip under the element with <paramref name="id"/>, shown as
    /// <see cref="Tooltip(Ui, int, string)"/> is, whose content is recorded inside the scope
    /// while <see cref="Ui.Scope.IsOpen"/>; dispose the scope either way. Call it every frame the
    /// element is recorded.</summary>
    public static Ui.Scope TooltipContent(this Ui ui, int id)
    {
        var look = TooltipLook.Of(ui.Theme);
        if (!ui.IsHovered(id))
            return OpenTooltip(ui, id, 0, default, look);
        if (!ui.TryGetRect(id, out var anchor))
            return default;
        return OpenTooltip(ui, id, id, anchor, look);
    }

    /// <summary>A tooltip for a part of the element <paramref name="owner"/> that the caller
    /// hit-tests itself, such as a word of painted text: shown under <paramref name="anchor"/>
    /// once the pointer has rested on part <paramref name="part"/> for the theme's tooltip delay,
    /// and again from the start when it moves to another part. Pass part 0 while no part is under
    /// the pointer. Content is recorded inside the scope while <see cref="Ui.Scope.IsOpen"/>; dispose
    /// the scope either way. <paramref name="look"/> sizes, pads and aligns the popup, the theme's
    /// tooltip look when null. Call it every frame the owner is recorded.</summary>
    public static Ui.Scope TooltipContent(this Ui ui, int owner, int part, UiRect anchor, TooltipLook? look = null)
    {
        var target = 0;
        if (part != 0 && ui.IsHovered(owner))
            target = HashCode.Combine(owner, part) | 1;
        var shown = look ?? TooltipLook.Of(ui.Theme);
        return OpenTooltip(ui, owner, target, anchor, shown);
    }

    /// <summary>Times the tooltip of <paramref name="target"/>, 0 while the owner has none under
    /// the pointer, and opens it once it is due.</summary>
    private static Ui.Scope OpenTooltip(Ui ui, int owner, int target, UiRect anchor, TooltipLook look)
    {
        var state = ui.GetState<TooltipState>(TooltipId);
        if (!ui.Input.IsForeground)
        {
            state.Target = 0;
            return default;
        }
        if (target == 0)
        {
            if (state.Owner == owner) state.Target = 0;
            return default;
        }
        if (state.Target != target)
        {
            state.Target = target;
            state.Owner = owner;
            state.Since = ui.RealTime;
            state.Suppressed = false;
        }
        var input = ui.Input;
        if (input.IsDown(PointerButton.Left) || input.WasPressed(PointerButton.Left) || input.WasPressed(PointerButton.Right))
            state.Suppressed = true;
        if (state.Suppressed) return default;

        var remaining = ui.Theme.TooltipDelaySeconds - (ui.RealTime - state.Since);
        if (remaining > 0)
        {
            ui.RequestFrameIn(remaining);
            return default;
        }

        var theme = ui.Theme;
        return ui.Popup(TooltipId, anchor, theme.PopupSurface(new Box
        {
            Width = Sizing.Fit(0f, look.MaxWidth),
            Padding = look.Padding,
        }), look.Align, gap: theme.TooltipGap, passThrough: true);
    }
}

/// <summary>How a tooltip popup is sized and placed: at most <paramref name="MaxWidth"/> wide,
/// <paramref name="Padding"/> inside its surface, aligned to the anchor by <paramref name="Align"/>.</summary>
public readonly record struct TooltipLook(float MaxWidth, Spacing Padding, Align Align)
{
    /// <summary>The theme's tooltip: its width and padding, centered under the anchor.</summary>
    public static TooltipLook Of(UiTheme theme) =>
        new(theme.TooltipMaxWidth, new Spacing(theme.TooltipPaddingX, theme.TooltipPaddingY), Align.Center);
}
