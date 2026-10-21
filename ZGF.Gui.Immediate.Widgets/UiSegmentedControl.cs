using ZGF.Desktop;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>One choice of a <see cref="UiWidgets.SegmentedControl{T}"/>.</summary>
public readonly record struct Segment<T>(T Value, string Label, string? Tooltip = null);

public static partial class UiWidgets
{
    /// <summary>A row of joined segments with the one matching <paramref name="value"/> selected,
    /// drawn in the theme's selection roles. Clicking a segment selects it; the group is a Tab stop,
    /// and once focused Left/Right/Home/End step through them. Returns true on the frame
    /// <paramref name="value"/> changes.</summary>
    public static bool SegmentedControl<T>(this Ui ui, string id, ref T value, ReadOnlySpan<Segment<T>> segments)
    {
        var theme = ui.Theme;
        var groupId = ui.GetId(id);
        var selected = -1;
        for (var i = 0; i < segments.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(segments[i].Value, value))
            {
                selected = i;
                break;
            }
        }

        var next = selected;
        var focused = ui.TabStop(groupId);
        if (focused && segments.Length > 0)
            next = SegmentKeys(ui, selected, segments.Length);

        var border = theme.BorderWidth;
        var inner = MathF.Max(0f, theme.CornerRadius - border);
        using (ui.Id(groupId))
        using (ui.Row(groupId, new Box
        {
            Width = Sizing.Fit(),
            Height = theme.ControlHeight,
            Gap = 0f,
            Padding = border,
            Background = theme.Control,
            BorderColor = theme.Border,
            BorderWidth = border,
            Radius = theme.CornerRadius,
        }))
        {
            if (focused && ui.FocusVisible) ui.FocusRing(theme.CornerRadius);
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = ui.Interact(ui.GetId(i));
                if (segment.Hovered) ui.SetCursor(MouseCursor.Hand);
                if (segment.Pressed) ui.Focus(groupId, takesText: false);
                if (segment.Clicked) next = i;

                if (i > 0)
                {
                    var touchesSelection = i == selected || i - 1 == selected;
                    ui.Leaf(new Box { Width = border, Height = Sizing.Grow(), Background = touchesSelection ? 0u : theme.Border });
                }

                var isSelected = i == selected;
                var background = isSelected ? theme.Selection
                    : segment.Held ? theme.ControlPressed
                    : segment.Hovered ? theme.ControlHovered
                    : 0u;
                var corners = segments.Length == 1 ? new Corners(inner)
                    : i == 0 ? Corners.Left(inner)
                    : i == segments.Length - 1 ? Corners.Right(inner)
                    : default;

                using (ui.Box(segment.Id, new Box
                {
                    Height = Sizing.Grow(),
                    Padding = new Spacing(theme.ControlPaddingX, 0f),
                    AlignY = Align.Center,
                }))
                {
                    if (background != 0)
                        ui.Paint((background, corners), static (p, s) => p.FillRect(p.Rect, s.background, s.corners));
                    ui.Text(segments[i].Label, color: isSelected ? theme.SelectionText : theme.TextMuted);
                }

                if (segments[i].Tooltip is { } tooltip)
                    ui.Tooltip(segment.Id, tooltip);
            }
        }

        if (next == selected || next < 0) return false;
        value = segments[next].Value;
        return true;
    }

    private static int SegmentKeys(Ui ui, int selected, int count)
    {
        var next = selected;
        foreach (var stroke in ui.Input.Keys)
        {
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            next = stroke.Key switch
            {
                KeyboardKey.LeftArrow => next < 0 ? 0 : Math.Max(0, next - 1),
                KeyboardKey.RightArrow => next < 0 ? 0 : Math.Min(count - 1, next + 1),
                KeyboardKey.Home => 0,
                KeyboardKey.End => count - 1,
                _ => next,
            };
        }
        return next;
    }
}
