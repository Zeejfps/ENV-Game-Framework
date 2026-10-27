using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Where a tooltip shows: centered under its element, or under the pointer where it rested
/// — for an element as wide as a table row, whose middle may be far from the pointer.</summary>
public enum TooltipAnchor
{
    Element,
    Pointer,
}

public static partial class UiWidgets
{
    private const int TooltipId = 0x544F4F4C;

    /// <summary>How tall the pointer is taken to be, so a tooltip shown under it clears it.</summary>
    private const float PointerHeight = 16f;
    private const double TooltipGraceSeconds = 0.2;
    private const double NotLeaving = -1;

    private sealed class TooltipState
    {
        public int Target;
        public int Owner;
        public int Part;
        public UiRect Anchor;
        public double Since;
        public double LeftAt = NotLeaving;
        public bool Shown;
        public bool Suppressed;
    }

    /// <summary>What a tooltip is for: the key it is timed by, 0 for none, the owner's part and
    /// where it is shown.</summary>
    private readonly record struct TooltipTarget(int Key, int Part, UiRect Anchor);

    public static void Tooltip(this Ui ui, Interaction target, string text) => ui.Tooltip(target.Id, text);

    /// <summary>Shows <paramref name="text"/> under the element with <paramref name="id"/>, or under
    /// the pointer as <paramref name="anchor"/> says, once the pointer has rested on it for the
    /// theme's tooltip delay, until it leaves or a button is pressed. Long text wraps into a block;
    /// <c>\n</c> breaks lines. Call it every frame the element is recorded.</summary>
    public static void Tooltip(this Ui ui, int id, string text, TooltipAnchor anchor = TooltipAnchor.Element)
    {
        using var tooltip = ui.TooltipContent(id, anchor);
        if (tooltip.IsOpen)
            ui.Text(text, color: ui.Theme.Text, size: ui.Theme.FontSizeSmall, wrap: true);
    }

    /// <summary>A tooltip under the element with <paramref name="id"/>, shown as
    /// <see cref="Tooltip(Ui, int, string)"/> is, whose content is recorded inside the scope
    /// while <see cref="Ui.Scope.IsOpen"/>; dispose the scope either way. Call it every frame the
    /// element is recorded.</summary>
    public static Ui.Scope TooltipContent(this Ui ui, int id, TooltipAnchor anchor = TooltipAnchor.Element)
    {
        var look = TooltipLook.Of(ui.Theme);
        if (!ui.IsHovered(id))
            return OpenTooltip(ui, id, new TooltipTarget(0, 0, default), look, out _);
        if (!ui.TryGetRect(id, out var element))
            return default;
        if (anchor == TooltipAnchor.Element)
            return OpenTooltip(ui, id, new TooltipTarget(id, id, element), look, out _);
        var underPointer = look with { Align = Align.Start };
        return OpenTooltip(ui, id, new TooltipTarget(id, id, PointerRect(ui)), underPointer, out _);
    }

    /// <summary>The pointer as a rect a tooltip shows under: as tall as the pointer, starting where it points.</summary>
    private static UiRect PointerRect(Ui ui)
    {
        var pointer = ui.Input.Pointer;
        return new UiRect(pointer.X, pointer.Y, 1f, PointerHeight);
    }

    /// <summary>As <see cref="TooltipContent(Ui, int, int, UiRect, out int, TooltipLook?)"/>, for a
    /// caller that only shows the part under the pointer.</summary>
    public static Ui.Scope TooltipContent(this Ui ui, int owner, int part, UiRect anchor, TooltipLook? look = null) =>
        ui.TooltipContent(owner, part, anchor, out _, look);

    /// <summary>A tooltip for a part of the element <paramref name="owner"/> that the caller
    /// hit-tests itself, such as a word of painted text: shown under <paramref name="anchor"/>
    /// once the pointer has rested on part <paramref name="part"/> for the theme's tooltip delay,
    /// and again from the start when it moves to another part. Pass part 0 while no part is under
    /// the pointer. Content is recorded inside the scope while <see cref="Ui.Scope.IsOpen"/>, for
    /// <paramref name="shownPart"/>; dispose the scope either way. <paramref name="look"/> sizes,
    /// pads and aligns the popup, the theme's tooltip look when null. An interactive look keeps the
    /// tooltip open while the pointer is over it, and for a moment after it leaves the part so it
    /// can get there, and closes it on Escape. Call it every frame the owner is recorded.</summary>
    public static Ui.Scope TooltipContent(this Ui ui, int owner, int part, UiRect anchor, out int shownPart, TooltipLook? look = null)
    {
        var key = 0;
        if (part != 0 && ui.IsHovered(owner))
            key = HashCode.Combine(owner, part) | 1;
        var shown = look ?? TooltipLook.Of(ui.Theme);
        return OpenTooltip(ui, owner, new TooltipTarget(key, part, anchor), shown, out shownPart);
    }

    /// <summary>As <see cref="TooltipContent(Ui, int, int, UiRect, out int, TooltipLook?)"/>, shown under
    /// the pointer where it rested on the part rather than under an anchor, starting there; it stays
    /// put while the pointer moves on.</summary>
    public static Ui.Scope TooltipContentAtPointer(this Ui ui, int owner, int part, out int shownPart, TooltipLook look) =>
        ui.TooltipContent(owner, part, PointerRect(ui), out shownPart, look with { Align = Align.Start });

    /// <summary>Closes the open tooltip until the pointer rests on another part, as after a link
    /// in it was followed.</summary>
    public static void HideTooltip(this Ui ui)
    {
        var state = ui.GetState<TooltipState>(TooltipId);
        Dismiss(state);
    }

    /// <summary>Times the tooltip of <paramref name="target"/> and opens it once it is due.</summary>
    private static Ui.Scope OpenTooltip(Ui ui, int owner, TooltipTarget target, TooltipLook look, out int shownPart)
    {
        shownPart = 0;
        var state = ui.GetState<TooltipState>(TooltipId);
        if (!ui.Input.IsForeground)
        {
            Close(state);
            return default;
        }
        if (look.Interactive && KeepsOpen(ui, state, owner, target.Key))
            target = new TooltipTarget(state.Target, state.Part, state.Anchor);
        if (target.Key == 0)
        {
            if (state.Owner == owner)
                Close(state);
            return default;
        }
        if (state.Target != target.Key)
            Start(ui, state, owner, target);
        if (!state.Shown)
            state.Anchor = target.Anchor;
        if (IsDismissed(ui, state, look))
            return default;

        var remaining = ui.Theme.TooltipDelaySeconds - (ui.RealTime - state.Since);
        if (remaining > 0)
        {
            ui.RequestUpdateIn(remaining);
            return default;
        }
        state.Shown = true;
        shownPart = state.Part;
        return ShowTooltip(ui, state, look);
    }

    /// <summary>Whether the tooltip <paramref name="owner"/> shows stays open though the pointer
    /// left its part: while the pointer is over the tooltip, and for a grace period after leaving,
    /// so it can cross to it.</summary>
    private static bool KeepsOpen(Ui ui, TooltipState state, int owner, int key)
    {
        if (state.Owner != owner)
            return false;
        if (!state.Shown || key == state.Target)
        {
            state.LeftAt = NotLeaving;
            return false;
        }
        if (IsPointerInTooltip(ui))
        {
            state.LeftAt = NotLeaving;
            return true;
        }
        if (state.LeftAt == NotLeaving)
            state.LeftAt = ui.RealTime;
        var remaining = TooltipGraceSeconds - (ui.RealTime - state.LeftAt);
        if (remaining <= 0)
            return false;
        ui.RequestUpdateIn(remaining);
        return true;
    }

    private static bool IsPointerInTooltip(Ui ui)
    {
        if (!ui.TryGetRect(TooltipId, out var rect))
            return false;
        return rect.Contains(ui.Input.Pointer);
    }

    private static void Start(Ui ui, TooltipState state, int owner, TooltipTarget target)
    {
        state.Target = target.Key;
        state.Owner = owner;
        state.Part = target.Part;
        state.Anchor = target.Anchor;
        state.Since = ui.RealTime;
        state.LeftAt = NotLeaving;
        state.Shown = false;
        state.Suppressed = false;
    }

    private static void Close(TooltipState state)
    {
        state.Target = 0;
        state.Shown = false;
        state.LeftAt = NotLeaving;
    }

    private static void Dismiss(TooltipState state)
    {
        state.Suppressed = true;
        state.Shown = false;
        state.LeftAt = NotLeaving;
    }

    /// <summary>Whether the tooltip was dismissed: by a press outside it, or by Escape while an
    /// interactive one is shown. It stays dismissed until another part is rested on.</summary>
    private static bool IsDismissed(Ui ui, TooltipState state, TooltipLook look)
    {
        if (IsPressedOutside(ui, look))
            Dismiss(state);
        if (look.Interactive && state.Shown && ui.Input.TakeKey(KeyboardKey.Escape))
            Dismiss(state);
        return state.Suppressed;
    }

    /// <summary>Whether a button is pressed or held, outside an interactive tooltip, whose own
    /// links take presses.</summary>
    private static bool IsPressedOutside(Ui ui, TooltipLook look)
    {
        var input = ui.Input;
        var pressed = input.IsDown(PointerButton.Left) || input.WasPressed(PointerButton.Left) || input.WasPressed(PointerButton.Right);
        if (!pressed)
            return false;
        if (!look.Interactive)
            return true;
        return !IsPointerInTooltip(ui);
    }

    /// <summary>The popup under the anchor, scrolling its content once taller than the look allows.</summary>
    private static Ui.Scope ShowTooltip(Ui ui, TooltipState state, TooltipLook look)
    {
        var theme = ui.Theme;
        var surface = theme.PopupSurface(new Box
        {
            Width = Sizing.Fit(0f, look.MaxWidth),
            Padding = look.Padding,
        });
        var popup = ui.Popup(TooltipId, state.Anchor, surface, look.Align, gap: theme.TooltipGap, passThrough: !look.Interactive);
        if (look.MaxHeight > 0f)
            ui.Scroll(HashCode.Combine(TooltipId, state.Target), new Box { Width = Sizing.Grow(), Height = Sizing.Fit(max: look.MaxHeight), Gap = 0f });
        return popup;
    }
}

/// <summary>How a tooltip popup is sized and placed: at most <paramref name="MaxWidth"/> wide,
/// <paramref name="Padding"/> inside its surface, aligned to the anchor by <paramref name="Align"/>.
/// An <paramref name="Interactive"/> one takes the pointer, so its content can be clicked, and
/// scrolls once taller than a positive <paramref name="MaxHeight"/>.</summary>
public readonly record struct TooltipLook(float MaxWidth, Spacing Padding, Align Align, bool Interactive = false, float MaxHeight = 0f)
{
    /// <summary>The theme's tooltip: its width and padding, centered under the anchor.</summary>
    public static TooltipLook Of(UiTheme theme) =>
        new(theme.TooltipMaxWidth, new Spacing(theme.TooltipPaddingX, theme.TooltipPaddingY), Align.Center);
}
