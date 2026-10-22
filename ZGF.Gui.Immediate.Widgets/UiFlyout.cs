using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const int FlyoutScrollSalt = 0x464C5953;

    /// <summary>
    /// A panel of any content below the element with <paramref name="ownerId"/>, shown while
    /// <paramref name="open"/>, which it clears on Escape, a press outside both the panel and the
    /// owner, or when the window dismisses its popups. Keys reach only what is recorded inside it
    /// while it is open; content taller than the theme's menu height scrolls. Record the content
    /// only while <see cref="Ui.Scope.IsOpen"/>, and dispose the scope either way.
    /// </summary>
    public static Ui.Scope Flyout(this Ui ui, int id, ref bool open, int ownerId, Box box = default)
    {
        if (!open) return default;
        var theme = ui.Theme;
        var input = ui.Input;
        var pointer = input.Pointer;
        var pressed = input.WasPressed(PointerButton.Left) || input.WasPressed(PointerButton.Right) || input.WasPressed(PointerButton.Middle);
        if (input.PopupsDismissed
            || pressed && !(ui.TryGetRect(id, out var panel) && panel.Contains(pointer)) && !(ui.TryGetRect(ownerId, out var owner) && owner.Contains(pointer)))
        {
            open = false;
            return default;
        }
        if (!ui.TryGetRect(ownerId, out var anchor))
        {
            open = false;
            return default;
        }

        var flyout = ui.Layer(id);
        if (ui.Input.TakeKey(KeyboardKey.Escape))
        {
            flyout.Dispose();
            open = false;
            return default;
        }
        ui.Popup(id, anchor, theme.PopupSurface(box with
        {
            Width = box.Width ?? Sizing.Fit(theme.MenuMinWidth),
            Padding = box.Padding ?? new Spacing(theme.TooltipPaddingX, theme.TooltipPaddingY),
        }), gap: theme.TooltipGap);
        ui.Scroll(HashCode.Combine(id, FlyoutScrollSalt), new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Fit(max: theme.MenuMaxHeight),
            Gap = box.Gap ?? theme.GapTight,
        });
        return flyout;
    }
}
