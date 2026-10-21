using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>
/// The built-in widgets. Each is an extension method over the public <see cref="Ui"/> surface
/// only, so an app's own widgets are written exactly the same way.
/// </summary>
public static partial class UiWidgets
{
    private const float CheckboxSize = 18f;
    private const float MinFieldWidth = 60f;
    private const float SliderThumbRadius = 8f;

    private sealed class LabelState
    {
        public string Visible = "";
    }

    public static void Label(this Ui ui, string text, uint? color = null, float? size = null, bool bold = false) =>
        ui.Text(text, color: color, size: size, bold: bold);

    public static void Heading(this Ui ui, string text) =>
        ui.Label(text, size: MathF.Round(ui.Style.FontSize * 1.5f), bold: true);

    /// <summary>A padded column on a raised background.</summary>
    public static Ui.Scope Panel(this Ui ui, Box box = default)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        return ui.Column(box with
        {
            Padding = box.Padding ?? theme.PanelPadding,
            Background = box.Background != 0 ? box.Background : style.Surface,
            BorderColor = box.BorderColor != 0 ? box.BorderColor : style.Border,
            BorderWidth = box.BorderWidth > 0f ? box.BorderWidth : 1f,
            Radius = box.Radius > 0f ? box.Radius : theme.CornerRadius,
        });
    }

    /// <summary>Pushes what follows to the end of the row or column, or leaves a fixed gap.</summary>
    public static void Spacer(this Ui ui, float? size = null)
    {
        var main = size is { } fixedSize ? Sizing.Fixed(fixedSize) : Sizing.Grow();
        ui.Leaf(ui.InRow ? new Box { Width = main, Height = 0f } : new Box { Width = 0f, Height = main });
    }

    public static void Separator(this Ui ui)
    {
        var color = ui.Style.Border;
        ui.Leaf(ui.InRow
            ? new Box { Width = 1f, Height = Sizing.Grow(), Background = color }
            : new Box { Width = Sizing.Grow(), Height = 1f, Background = color });
    }

    /// <summary>Returns true on the frame the button is clicked. The label doubles as the id unless
    /// <paramref name="id"/> is given, or the label carries a hidden suffix after <c>##</c>:
    /// <c>"Save##toolbar"</c>.</summary>
    public static bool Button(this Ui ui, string label, Sizing? width = null, bool enabled = true, string? id = null)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);

        ui.Text(id != null ? label : Visible(ui, i.Id, label), new Box
        {
            Width = width ?? Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Background = i.Held ? style.Pressed(style.Control) : i.Hovered ? style.Hovered(style.Control) : style.Control,
            BorderColor = enabled ? style.Border : style.Control,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }, color: enabled ? style.Text : style.TextMuted, align: Align.Center, id: i.Id);
        return i.Clicked;
    }

    /// <summary>Returns true on the frame <paramref name="value"/> changes. Ids work as for
    /// <see cref="Button"/>.</summary>
    public static bool Checkbox(this Ui ui, string label, ref bool value, uint? textColor = null, string? id = null)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var i = ui.Interact(id ?? label);
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);
        if (i.Clicked) value = !value;

        using (ui.Row(i.Id, new Box { Width = Sizing.Fit(), Height = theme.ControlHeight }))
        {
            using (ui.Box(new Box
            {
                Width = CheckboxSize,
                Height = CheckboxSize,
                Background = value ? style.Accent : i.Hovered ? style.Hovered(style.Field) : style.Field,
                BorderColor = value ? style.Accent : i.Hovered ? style.TextMuted : style.Border,
                BorderWidth = 1f,
                Radius = 4f,
            }))
            {
                if (value)
                    ui.Paint(style.OnAccent, static (p, color) => DrawCheckmark(p, color));
            }
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: textColor ?? style.Text);
        }
        return i.Clicked;
    }

    /// <summary>Returns true on frames where <paramref name="value"/> changes.</summary>
    public static bool Slider(this Ui ui, string id, ref float value, float min, float max, Sizing? width = null)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
        var i = ui.Interact(id);
        if (i.Hovered || i.Held) ui.SetCursor(MouseCursor.Hand);

        var changed = false;
        if (i.Held && max > min && ui.TryGetRect(i.Id, out var rect))
        {
            var track = MathF.Max(1f, rect.Width - SliderThumbRadius * 2);
            var t = Math.Clamp((ui.Input.Pointer.X - rect.X - SliderThumbRadius) / track, 0f, 1f);
            var next = min + t * (max - min);
            changed = next != value;
            value = next;
        }

        var fraction = max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = theme.ControlHeight,
            Padding = new Spacing(SliderThumbRadius, 0f),
        }))
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = 4f, Background = style.Control, Radius = 2f }))
        {
            ui.Leaf(new Box { Width = Sizing.Percent(fraction), Height = Sizing.Grow(), Background = style.Accent, Radius = 2f });
            using (ui.Float(fraction, 0.5f, -SliderThumbRadius, -SliderThumbRadius, new Box
            {
                Width = SliderThumbRadius * 2,
                Height = SliderThumbRadius * 2,
                Background = style.OnAccent,
                BorderColor = i.Held || i.Hovered ? style.Accent : style.Border,
                BorderWidth = 2f,
                Radius = SliderThumbRadius,
            }, z: 1)) { }
        }
        return changed;
    }

    public static void ProgressBar(this Ui ui, float fraction, Sizing? width = null)
    {
        var style = ui.Style;
        using (ui.Box(new Box { Width = width ?? Sizing.Grow(), Height = 8f, Background = style.Control, Radius = 4f }))
            ui.Leaf(new Box { Width = Sizing.Percent(Math.Clamp(fraction, 0f, 1f)), Height = Sizing.Grow(), Background = style.Accent, Radius = 4f });
    }

    private static void DrawCheckmark(UiPainter p, uint color)
    {
        var r = p.Rect;
        var start = new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.5f);
        var corner = new PointF(r.X + r.Width * 0.42f, r.Y + r.Height * 0.68f);
        var end = new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.32f);
        p.Line(start, corner, 2f, color);
        p.Line(corner, end, 2f, color);
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
