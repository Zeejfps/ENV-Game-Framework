using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>
/// The built-in widgets. Each is an extension method over the public <see cref="Ui"/> surface
/// only, so an app's own widgets are written exactly the same way.
/// </summary>
public static partial class UiWidgets
{
    private const float MinFieldWidth = 60f;
    private const int SliderKeySteps = 20;

    private sealed class LabelState
    {
        public string Visible = "";
    }

    public static void Label(this Ui ui, string text, uint? color = null, float? size = null, bool bold = false) =>
        ui.Text(text, color: color, size: size, bold: bold);

    /// <summary>Secondary text: <see cref="UiTheme.FontSizeSmall"/> in <see cref="UiTheme.TextMuted"/>.</summary>
    public static void Caption(this Ui ui, string text, uint? color = null) =>
        ui.Text(text, color: color ?? ui.Theme.TextMuted, size: ui.Theme.FontSizeSmall);

    /// <summary>A title: <see cref="UiTheme.FontSizeTitle"/>, bold.</summary>
    public static void Heading(this Ui ui, string text) =>
        ui.Label(text, size: ui.Theme.FontSizeTitle, bold: true);

    /// <summary>A column filling the window on the theme's background, padded by its
    /// <see cref="UiTheme.ScreenPadding"/>.</summary>
    public static Ui.Scope Screen(this Ui ui)
    {
        var theme = ui.Theme;
        ui.WindowBackground = theme.Background;
        return ui.Column(new Box
        {
            Width = Sizing.Grow(),
            Height = Sizing.Grow(),
            Padding = theme.ScreenPadding,
            Background = theme.Background,
        });
    }

    /// <summary>Provides <paramref name="theme"/> and opens a <see cref="Screen(Ui)"/> in it.</summary>
    public static ScreenScope Screen(this Ui ui, UiTheme theme)
    {
        var context = ui.Provide(theme);
        return new ScreenScope(context, ui.Screen());
    }

    /// <summary>A padded, bordered column on the theme's surface. Set the box's border width or
    /// radius to zero for a borderless or square one.</summary>
    public static Ui.Scope Panel(this Ui ui, Box box = default) => ui.Panel(0, box);

    /// <summary>A <see cref="Panel(Ui, Box)"/> hit-testable and measurable as <paramref name="id"/>.</summary>
    public static Ui.Scope Panel(this Ui ui, int id, Box box = default)
    {
        var theme = ui.Theme;
        return ui.Column(id, box with
        {
            Padding = box.Padding ?? theme.PanelPadding,
            Background = box.Background ?? theme.Surface,
            BorderColor = box.BorderColor ?? theme.Border,
            BorderWidth = box.BorderWidth ?? theme.BorderWidth,
            Radius = box.Radius ?? theme.PanelRadius,
        });
    }

    /// <summary>Pushes what follows to the end of the row or column, or leaves a fixed gap.</summary>
    public static void Spacer(this Ui ui, float? size = null)
    {
        var main = size is { } fixedSize ? Sizing.Fixed(fixedSize) : Sizing.Grow();
        ui.Leaf(ui.InRow ? new Box { Width = main, Height = 0f } : new Box { Width = 0f, Height = main });
    }

    /// <summary>A structural rule across the row or column, in <see cref="UiTheme.Separator"/>.</summary>
    public static void Separator(this Ui ui)
    {
        var theme = ui.Theme;
        ui.Leaf(ui.InRow
            ? new Box { Width = theme.SeparatorWidth, Height = Sizing.Grow(), Background = theme.Separator }
            : new Box { Width = Sizing.Grow(), Height = theme.SeparatorWidth, Background = theme.Separator });
    }

    /// <summary>Returns true on the frame the button is clicked, or activated with Enter or Space
    /// while it holds keyboard focus, which Tab gives it. The label doubles as the id unless
    /// <paramref name="id"/> is given, or the label carries a hidden suffix after <c>##</c>:
    /// <c>"Save##toolbar"</c>. An <paramref name="icon"/> leads the label, in the label's color
    /// unless it has its own.</summary>
    public static bool Button(this Ui ui, string label, Sizing? width = null, bool enabled = true, string? id = null,
        ButtonVariant variant = ButtonVariant.Neutral, UiIcon? icon = null)
    {
        var theme = ui.Theme;
        var key = id ?? label;
        var i = Pressable(ui, key, enabled, out var activated, out var ring);
        var (fill, border, text) = ButtonColors(theme, variant, enabled, i.Held || activated, i.Hovered);

        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Gap = theme.GapTight,
            AlignX = Align.Center,
            AlignY = Align.Center,
            Background = fill,
            BorderColor = border,
            BorderWidth = border != 0 ? theme.BorderWidth : 0f,
            Radius = theme.CornerRadius,
        }))
        {
            if (icon is { } shown)
                ui.Icon(shown.Color != 0 && enabled ? shown : shown with { Color = text });
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: text, overflow: width != null ? TextOverflow.Ellipsis : TextOverflow.Clip);
            if (ring) FocusRing(ui, theme.CornerRadius);
        }
        return i.Clicked || activated;
    }

    /// <summary>Returns true on the frame <paramref name="value"/> changes, by a click or by Space
    /// while focused. Ids work as for <see cref="Button"/>. When not <paramref name="enabled"/> it
    /// shows <paramref name="value"/> greyed out and ignores input. Given a <paramref name="width"/>,
    /// a label too long for it ends in an ellipsis. A <paramref name="bold"/> label stands out, e.g.
    /// as a choice changed from its default.</summary>
    public static bool Checkbox(this Ui ui, string label, ref bool value, uint? textColor = null, string? id = null, bool enabled = true,
        Sizing? width = null, bool bold = false)
    {
        if (!DrawCheckbox(ui, label, value, textColor, id, enabled, width, bold)) return false;
        value = !value;
        return true;
    }

    /// <summary>A checkbox for a group of choices: <paramref name="value"/> is null when only some
    /// are on, shown as a filled square. A click turns it on unless it is already, then off; returns
    /// true on that frame. Otherwise as the two-state <see cref="Checkbox(Ui, string, ref bool, uint?, string?, bool, Sizing?, bool)"/>.</summary>
    public static bool Checkbox(this Ui ui, string label, ref bool? value, uint? textColor = null, string? id = null, bool enabled = true,
        Sizing? width = null, bool bold = false)
    {
        if (!DrawCheckbox(ui, label, value, textColor, id, enabled, width, bold)) return false;
        value = value != true;
        return true;
    }

    private static bool DrawCheckbox(Ui ui, string label, bool? value, uint? textColor, string? id, bool enabled, Sizing? width, bool bold)
    {
        var theme = ui.Theme;
        var i = Pressable(ui, id ?? label, enabled, out var activated, out var ring);
        var on = value == true;
        var mixed = value is null;

        using (ui.Row(i.Id, new Box { Width = width ?? Sizing.Fit(), Height = theme.ControlHeight, Gap = theme.Gap }))
        {
            var size = theme.CheckboxSize;
            var accent = !enabled ? theme.TextDisabled : i.Hovered ? theme.AccentHovered : theme.Accent;
            using (ui.Box(new Box
            {
                Width = size,
                Height = size,
                AlignX = mixed ? Align.Center : default,
                AlignY = mixed ? Align.Center : default,
                Background = on ? accent : theme.Field,
                BorderColor = on ? 0u : i.Hovered ? theme.BorderStrong : theme.Border,
                BorderWidth = on ? 0f : theme.BorderWidth,
                Radius = theme.RadiusSmall,
            }))
            {
                if (on)
                    ui.Paint((theme.OnAccent, theme.IconStrokeWidth), static (p, s) => DrawCheckmark(p, s.OnAccent, s.IconStrokeWidth));
                else if (mixed)
                    ui.Leaf(new Box { Width = size / 2f, Height = size / 2f, Background = accent, Radius = theme.RadiusSmall / 2f });
                if (ring) FocusRing(ui, theme.RadiusSmall);
            }
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: !enabled ? theme.TextDisabled : textColor ?? theme.Text, bold: bold,
                overflow: width != null ? TextOverflow.Ellipsis : TextOverflow.Clip);
        }
        return i.Clicked || activated;
    }

    /// <summary>Returns true on frames where <paramref name="value"/> changes: by dragging, or by
    /// Left/Right (a twentieth of the range), Home and End while focused.</summary>
    public static bool Slider(this Ui ui, string id, ref float value, float min, float max, Sizing? width = null)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id);
        if (i.Hovered || i.Held) ui.SetCursor(MouseCursor.Hand);
        var focused = ui.TabStop(i.Id);
        var radius = theme.SliderThumbSize / 2;

        var before = value;
        if (i.Held && max > min && ui.TryGetRect(i.Id, out var rect))
        {
            var track = MathF.Max(1f, rect.Width - radius * 2);
            var t = Math.Clamp((ui.Input.Pointer.X - rect.X - radius) / track, 0f, 1f);
            value = min + t * (max - min);
        }
        else if (focused && max > min)
        {
            value = SliderKeys(ui, value, min, max);
        }

        var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
        var ring = focused && ui.FocusVisible;
        var trackHeight = theme.SliderTrackHeight;
        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = theme.ControlHeight,
            Padding = new Spacing(radius, 0f),
        }))
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = trackHeight, Background = theme.Control, Radius = trackHeight / 2 }))
        {
            ui.Leaf(new Box { Width = Sizing.Percent(fraction), Height = Sizing.Grow(), Background = theme.Accent, Radius = trackHeight / 2 });
            using (ui.Float(fraction, 0.5f, -radius, -radius, new Box
            {
                Width = theme.SliderThumbSize,
                Height = theme.SliderThumbSize,
                Background = theme.OnAccent,
                BorderColor = ring ? theme.FocusRing : i.Held || i.Hovered ? theme.Accent : theme.BorderStrong,
                BorderWidth = ring ? theme.FocusRingWidth : theme.BorderWidth,
                Radius = radius,
            }, z: 1)) { }
        }
        return value != before;
    }

    public static void ProgressBar(this Ui ui, float fraction, Sizing? width = null)
    {
        var theme = ui.Theme;
        var height = theme.ProgressHeight;
        using (ui.Box(new Box { Width = width ?? Sizing.Grow(), Height = height, Background = theme.Control, Radius = height / 2 }))
            ui.Leaf(new Box { Width = Sizing.Percent(Math.Clamp(fraction, 0f, 1f)), Height = Sizing.Grow(), Background = theme.Accent, Radius = height / 2 });
    }

    /// <summary>Paints <see cref="UiTheme.FocusRing"/> inside the element being recorded, rounded by
    /// <paramref name="radius"/>, without changing its layout — for a widget of an app's own that
    /// holds focus while <see cref="Ui.FocusVisible"/>.</summary>
    public static void FocusRing(this Ui ui, float radius)
    {
        var theme = ui.Theme;
        ui.Paint((theme.FocusRing, theme.FocusRingWidth, theme.FocusRingInset, radius), static (p, s) =>
        {
            var r = p.Rect;
            var inset = s.FocusRingInset;
            p.FillRect(new UiRect(r.X + inset, r.Y + inset, r.Width - inset * 2, r.Height - inset * 2), 0u, MathF.Max(0f, s.radius - inset),
                s.FocusRing, s.FocusRingWidth);
        });
    }

    // The shared behavior of a clickable control: pointer interaction, a Tab stop, Enter/Space
    // activation while focused, the hand cursor, and whether to draw the focus ring.
    private static Interaction Pressable(Ui ui, string key, bool enabled, out bool activated, out bool ring) =>
        Pressable(ui, ui.GetId(key), enabled, out activated, out ring);

    private static Interaction Pressable(Ui ui, int id, bool enabled, out bool activated, out bool ring, bool hand = true)
    {
        if (!enabled)
        {
            activated = ring = false;
            return Interaction.Inert(id);
        }
        var i = ui.Interact(id);
        if (hand && i.Hovered) ui.SetCursor(MouseCursor.Hand);
        var focused = ui.TabStop(i.Id);
        activated = focused && Activated(ui);
        ring = focused && ui.FocusVisible;
        return i;
    }

    private static bool Activated(Ui ui)
    {
        var input = ui.Input;
        var enter = input.TakeKey(KeyboardKey.Enter);
        var space = input.TakeKey(KeyboardKey.Space);
        return enter || space;
    }

    private static (uint Fill, uint Border, uint Text) ButtonColors(UiTheme theme, ButtonVariant variant, bool enabled, bool pressed, bool hovered)
    {
        if (!enabled)
            return variant == ButtonVariant.Ghost ? (0u, 0u, theme.TextDisabled) : (theme.Control, 0u, theme.TextDisabled);
        return variant switch
        {
            ButtonVariant.Primary => (pressed ? theme.AccentPressed : hovered ? theme.AccentHovered : theme.Accent, 0u, theme.OnAccent),
            ButtonVariant.Ghost => (pressed ? theme.ControlPressed : hovered ? theme.ControlHovered : 0u, 0u, theme.Text),
            _ => (pressed ? theme.ControlPressed : hovered ? theme.ControlHovered : theme.Control, theme.Border, theme.Text),
        };
    }

    private static float SliderKeys(Ui ui, float value, float min, float max)
    {
        var step = (max - min) / SliderKeySteps;
        foreach (var stroke in ui.Input.Keys)
        {
            if ((stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Super)) != 0) continue;
            value = stroke.Key switch
            {
                KeyboardKey.LeftArrow or KeyboardKey.DownArrow => Math.Max(min, value - step),
                KeyboardKey.RightArrow or KeyboardKey.UpArrow => Math.Min(max, value + step),
                KeyboardKey.Home => min,
                KeyboardKey.End => max,
                _ => value,
            };
        }
        return value;
    }

    private static void DrawCheckmark(UiPainter p, uint color, float stroke)
    {
        var r = p.Rect;
        var start = new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.5f);
        var corner = new PointF(r.X + r.Width * 0.42f, r.Y + r.Height * 0.68f);
        var end = new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.32f);
        p.Line(start, corner, stroke, color);
        p.Line(corner, end, stroke, color);
    }

    // Strips a "##id" suffix. The stripped text is kept in the widget's state and reused while it
    // still matches, so a hidden-id label doesn't allocate a fresh substring every frame.
    private static string Visible(Ui ui, int id, string label)
    {
        var hidden = label.IndexOf("##", StringComparison.Ordinal);
        if (hidden < 0) return label;

        var state = ui.GetState<LabelState>(id);
        if (!state.Visible.AsSpan().SequenceEqual(label.AsSpan(0, hidden)))
            state.Visible = label[..hidden];
        return state.Visible;
    }
}
