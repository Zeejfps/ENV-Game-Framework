using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>The content of a <see cref="UiWidgets.Flyout"/>: record it only while
/// <see cref="IsOpen"/>, and dispose the scope either way.</summary>
public readonly struct FlyoutScope : IDisposable
{
    private readonly Ui.Scope _layer;
    private readonly Ui.Scope _popup;
    private readonly Ui.Scope _scroll;

    internal FlyoutScope(Ui.Scope layer, Ui.Scope popup, Ui.Scope scroll)
    {
        _layer = layer;
        _popup = popup;
        _scroll = scroll;
        IsOpen = true;
    }

    public bool IsOpen { get; }

    public void Dispose()
    {
        if (!IsOpen) return;
        _scroll.Dispose();
        _popup.Dispose();
        _layer.Dispose();
    }
}

public static partial class UiWidgets
{
    private const int FlyoutScrollSalt = 0x464C5953;

    /// <summary>
    /// A panel of any content below the element with <paramref name="ownerId"/>, shown while
    /// <paramref name="open"/>, which it clears on Escape, a press outside both the panel and the
    /// owner, or when the window dismisses its popups. Keys reach only what is recorded inside it
    /// while it is open; content taller than the theme's menu height scrolls. Record the content
    /// only while <see cref="FlyoutScope.IsOpen"/>.
    /// </summary>
    public static FlyoutScope Flyout(this Ui ui, int id, ref bool open, int ownerId, Box box = default)
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

        var layer = ui.Layer(id);
        if (ui.Input.TakeKey(KeyboardKey.Escape))
        {
            layer.Dispose();
            open = false;
            return default;
        }
        var popup = ui.Popup(id, anchor, box with
        {
            Width = box.Width ?? Sizing.Fit(theme.MenuMinWidth),
            Padding = box.Padding ?? new Spacing(theme.TooltipPaddingX, theme.TooltipPaddingY),
            Background = box.Background ?? theme.SurfaceRaised,
            BorderColor = box.BorderColor ?? theme.Border,
            BorderWidth = box.BorderWidth ?? theme.BorderWidth,
            Radius = box.Radius ?? theme.PopupRadius,
            Shadow = box.Shadow ?? theme.PopupShadow,
        }, gap: theme.TooltipGap);
        var scroll = ui.Scroll(HashCode.Combine(id, FlyoutScrollSalt), new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Fit(max: theme.MenuMaxHeight),
            Gap = box.Gap ?? theme.GapTight,
        });
        return new FlyoutScope(layer, popup, scroll);
    }
}
