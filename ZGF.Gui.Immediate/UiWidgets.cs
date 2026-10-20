using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate;

/// <summary>
/// The built-in widgets. Each is an extension method over the public <see cref="Ui"/> surface
/// only, so an app's own widgets are written exactly the same way.
/// </summary>
public static class UiWidgets
{
    private const float CheckboxSize = 18f;
    private const float FieldPaddingX = 8f;
    private const float MinFieldWidth = 60f;
    private const float SliderThumbRadius = 8f;

    private sealed class TextFieldState
    {
        public int Caret;
        public float Shift;
    }

    private sealed class LabelState
    {
        public string Visible = "";
    }

    public static void Label(this Ui ui, string text, uint? color = null, float? size = null, bool bold = false) =>
        ui.Text(text, color: color, size: size, bold: bold);

    public static void Heading(this Ui ui, string text) =>
        ui.Label(text, size: MathF.Round(ui.Theme.FontSize * 1.5f), bold: true);

    /// <summary>A padded column on a raised background.</summary>
    public static Ui.Scope Panel(this Ui ui, Box box = default) => ui.Column(box with
    {
        Padding = box.Padding ?? ui.Theme.PanelPadding,
        Background = box.Background != 0 ? box.Background : ui.Theme.Panel,
        BorderColor = box.BorderColor != 0 ? box.BorderColor : ui.Theme.PanelBorder,
        BorderWidth = box.BorderWidth > 0f ? box.BorderWidth : 1f,
        Radius = box.Radius > 0f ? box.Radius : ui.Theme.CornerRadius,
    });

    /// <summary>Pushes what follows to the end of the row or column, or leaves a fixed gap.</summary>
    public static void Spacer(this Ui ui, float? size = null)
    {
        var main = size is { } fixedSize ? Sizing.Fixed(fixedSize) : Sizing.Grow();
        ui.Leaf(ui.InRow ? new Box { Width = main, Height = 0f } : new Box { Width = 0f, Height = main });
    }

    public static void Separator(this Ui ui)
    {
        var color = ui.Theme.PanelBorder;
        ui.Leaf(ui.InRow
            ? new Box { Width = 1f, Height = Sizing.Grow(), Background = color }
            : new Box { Width = Sizing.Grow(), Height = 1f, Background = color });
    }

    /// <summary>Returns true on the frame the button is clicked. The label doubles as the id unless
    /// <paramref name="id"/> is given, or the label carries a hidden suffix after <c>##</c>:
    /// <c>"Save##toolbar"</c>.</summary>
    public static bool Button(this Ui ui, string label, Sizing? width = null, bool enabled = true, string? id = null)
    {
        var theme = ui.Theme;
        var key = id ?? label;
        var i = enabled ? ui.Interact(key) : Interaction.Inert(ui.GetId(key));
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);

        ui.Text(id != null ? label : Visible(ui, i.Id, label), new Box
        {
            Width = width ?? Sizing.Fit(),
            Height = theme.ControlHeight,
            Padding = new Spacing(theme.ControlPaddingX, 0f),
            Background = !enabled || i.Held ? theme.ControlActive : i.Hovered ? theme.ControlHovered : theme.Control,
            BorderColor = enabled ? theme.ControlBorder : theme.Control,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }, color: enabled ? theme.Text : theme.TextMuted, align: Align.Center, id: i.Id);
        return i.Clicked;
    }

    /// <summary>Returns true on the frame <paramref name="value"/> changes. Ids work as for
    /// <see cref="Button"/>.</summary>
    public static bool Checkbox(this Ui ui, string label, ref bool value, uint? textColor = null, string? id = null)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id ?? label);
        if (i.Hovered) ui.SetCursor(MouseCursor.Hand);
        if (i.Clicked) value = !value;

        using (ui.Row(i.Id, new Box { Width = Sizing.Fit(), Height = theme.ControlHeight }))
        {
            using (ui.Box(new Box
            {
                Width = CheckboxSize,
                Height = CheckboxSize,
                Background = value ? theme.Accent : i.Hovered ? theme.ControlHovered : theme.Control,
                BorderColor = value ? theme.Accent : theme.ControlBorder,
                BorderWidth = 1f,
                Radius = 4f,
            }))
            {
                if (value)
                    ui.Paint(theme.OnAccent, static (p, color) => DrawCheckmark(p, color));
            }
            ui.Text(id != null ? label : Visible(ui, i.Id, label), color: textColor ?? theme.Text);
        }
        return i.Clicked;
    }

    /// <summary>Edits <paramref name="text"/> in place. Returns true when Enter is pressed while
    /// the field has focus.</summary>
    public static bool TextField(this Ui ui, string id, ref string text, string? placeholder = null, Sizing? width = null)
    {
        var theme = ui.Theme;
        var i = ui.Interact(id);
        if (i.Hovered) ui.SetCursor(MouseCursor.Text);

        var state = ui.GetState<TextFieldState>(i.Id);
        var focused = ui.HasFocus(i.Id);
        var innerWidth = ui.TryGetRect(i.Id, out var rect) ? rect.Width - FieldPaddingX * 2 : float.PositiveInfinity;
        if (i.Pressed)
        {
            if (!focused) state.Shift = 0f;
            ui.Focus(i.Id);
            focused = true;
            state.Caret = CaretAt(ui, text, ui.Input.Pointer.X - rect.X - FieldPaddingX + state.Shift);
        }

        var submitted = false;
        var caretX = 0f;
        if (focused)
        {
            submitted = Edit(ui, state, ref text);
            focused = ui.HasFocus(i.Id);
            state.Caret = Math.Clamp(state.Caret, 0, text.Length);
            caretX = ui.MeasureText(text, state.Caret);
            if (caretX - state.Shift > innerWidth) state.Shift = caretX - innerWidth;
            if (caretX < state.Shift) state.Shift = caretX;
            state.Shift = MathF.Max(0f, MathF.Min(state.Shift, ui.MeasureText(text) - innerWidth));
        }
        var shift = focused ? state.Shift : 0f;

        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = theme.ControlHeight,
            Padding = new Spacing(FieldPaddingX, 0f),
            Clip = true,
            ScrollX = shift,
            Background = theme.FieldBackground,
            BorderColor = focused ? theme.Accent : i.Hovered ? theme.TextMuted : theme.ControlBorder,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }))
        {
            if (text.Length > 0)
                ui.Text(text);
            else if (placeholder != null)
                ui.Text(placeholder, color: theme.TextMuted);

            if (focused)
            {
                var lineHeight = ui.LineHeight();
                using (ui.Float(0f, 0.5f, FieldPaddingX + caretX - shift, -lineHeight / 2,
                    new Box { Width = 1.5f, Height = lineHeight, Background = theme.Text }, z: 1)) { }
            }
        }
        return submitted;
    }

    /// <summary>Returns true on frames where <paramref name="value"/> changes.</summary>
    public static bool Slider(this Ui ui, string id, ref float value, float min, float max, Sizing? width = null)
    {
        var theme = ui.Theme;
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
        using (ui.Box(new Box { Width = Sizing.Grow(), Height = 4f, Background = theme.Control, Radius = 2f }))
        {
            ui.Leaf(new Box { Width = Sizing.Percent(fraction), Height = Sizing.Grow(), Background = theme.Accent, Radius = 2f });
            using (ui.Float(fraction, 0.5f, -SliderThumbRadius, -SliderThumbRadius, new Box
            {
                Width = SliderThumbRadius * 2,
                Height = SliderThumbRadius * 2,
                Background = i.Held || i.Hovered ? theme.OnAccent : theme.Text,
                Radius = SliderThumbRadius,
            }, z: 1)) { }
        }
        return changed;
    }

    public static void ProgressBar(this Ui ui, float fraction, Sizing? width = null)
    {
        var theme = ui.Theme;
        using (ui.Box(new Box { Width = width ?? Sizing.Grow(), Height = 8f, Background = theme.Control, Radius = 4f }))
            ui.Leaf(new Box { Width = Sizing.Percent(Math.Clamp(fraction, 0f, 1f)), Height = Sizing.Grow(), Background = theme.Accent, Radius = 4f });
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

    private static bool Edit(Ui ui, TextFieldState state, ref string text)
    {
        var submitted = false;
        state.Caret = Math.Clamp(state.Caret, 0, text.Length);
        foreach (var stroke in ui.Input.Keys)
        {
            var shortcut = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            switch (stroke.Key)
            {
                case KeyboardKey.Backspace when state.Caret > 0:
                {
                    var n = StepBack(text, state.Caret);
                    text = text.Remove(state.Caret - n, n);
                    state.Caret -= n;
                    break;
                }
                case KeyboardKey.Delete when state.Caret < text.Length:
                    text = text.Remove(state.Caret, StepForward(text, state.Caret));
                    break;
                case KeyboardKey.LeftArrow when state.Caret > 0:
                    state.Caret -= StepBack(text, state.Caret);
                    break;
                case KeyboardKey.RightArrow when state.Caret < text.Length:
                    state.Caret += StepForward(text, state.Caret);
                    break;
                case KeyboardKey.Home:
                    state.Caret = 0;
                    break;
                case KeyboardKey.End:
                    state.Caret = text.Length;
                    break;
                case KeyboardKey.Enter:
                    submitted = true;
                    break;
                case KeyboardKey.Escape:
                    ui.ClearFocus();
                    return submitted;
                case KeyboardKey.V when shortcut:
                    Insert(state, ref text, ui.ClipboardText.ReplaceLineEndings(" "));
                    break;
            }
        }

        if (ui.Input.Text.Length > 0)
            Insert(state, ref text, ui.Input.Text);
        return submitted;
    }

    private static void Insert(TextFieldState state, ref string text, string value)
    {
        text = text.Insert(state.Caret, value);
        state.Caret += value.Length;
    }

    private static int CaretAt(Ui ui, string text, float localX)
    {
        var best = 0;
        var bestDistance = MathF.Abs(localX);
        for (var index = 1; index <= text.Length; index++)
        {
            if (index < text.Length && char.IsLowSurrogate(text[index])) continue;
            var distance = MathF.Abs(ui.MeasureText(text, index) - localX);
            if (distance >= bestDistance) continue;
            best = index;
            bestDistance = distance;
        }
        return best;
    }

    private static int StepBack(string text, int caret) =>
        caret >= 2 && char.IsSurrogatePair(text[caret - 2], text[caret - 1]) ? 2 : 1;

    private static int StepForward(string text, int caret) =>
        caret + 1 < text.Length && char.IsSurrogatePair(text[caret], text[caret + 1]) ? 2 : 1;

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
