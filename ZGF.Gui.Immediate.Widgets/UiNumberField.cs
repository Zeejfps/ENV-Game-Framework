using System.Globalization;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    // How far the pointer moves along a number field for each step its value takes while scrubbed, and how far it
    // must go before a press becomes a scrub rather than a click that starts typing.
    private const float ScrubPixelsPerStep = 6f;
    private const float ScrubThreshold = 3f;

    // A held arrow steps once, then again after the delay and every interval after.
    private const double ArrowRepeatDelay = 0.4;
    private const double ArrowRepeatInterval = 0.06;

    private sealed class NumberFieldEdit
    {
        public bool Typing;
        public string Text = "";
        public NumberFieldPart Pressed;
        public float PressX;
        public int PressValue;
        public bool Scrubbing;
        public double NextRepeat;
        public float Wheel;
    }

    private const string NumberTextName = "text";

    /// <summary>The scope the text field a number field <paramref name="fieldId"/> is typed in is recorded in, found
    /// in tests under the number field.</summary>
    private static Ui.Scope NumberTextScope(Ui ui, int fieldId) => ui.UniqueId(HashCode.Combine(fieldId, "number-text"), fieldId);

    /// <summary>The id of the text field a number field <paramref name="fieldId"/> is typed in.</summary>
    internal static int NumberTextId(Ui ui, int fieldId)
    {
        using (NumberTextScope(ui, fieldId))
            return ui.GetId(NumberTextName);
    }

    /// <summary>
    /// Edits a whole number <paramref name="value"/> between <paramref name="min"/> and <paramref name="max"/>, as an
    /// animation program's frame or a property's spinner does. It shows the value against its end in tabular figures,
    /// <paramref name="width"/> wide, with an arrow at each end while the pointer is over it. An arrow steps the value
    /// one way, again and again while held; dragging the value sideways scrubs it, a step every few points; a click
    /// on it, or Enter while it has the focus, types over it, the whole value selected, Enter or the focus leaving
    /// taking what was typed (kept in range) and Escape dropping it. The wheel over it, or Up and Down while it has the
    /// focus, step it. Returns true on the frame the value changes. It is drawn by the theme's
    /// <see cref="UiTheme.NumberFieldPainter"/>, or as <see cref="PaintNumberField"/> draws one, and while typed in it
    /// is a <see cref="TextField"/>.
    /// </summary>
    public static bool NumberField(this Ui ui, string id, ref int value, int min, int max, float width, string? tooltip = null,
        bool enabled = true)
    {
        if (max < min)
            throw new ArgumentOutOfRangeException(nameof(max), max, $"The most a number field takes is less than its least, {min}.");
        var fieldId = ui.GetId(id);
        var edit = ui.GetState<NumberFieldEdit>(HashCode.Combine(fieldId, "number-edit"));
        var before = value;
        value = Math.Clamp(value, min, max);
        if (edit.Typing && enabled)
        {
            value = Type(ui, fieldId, edit, value, min, max, width);
            if (tooltip != null)
                ui.Tooltip(NumberTextId(ui, fieldId), tooltip);
            return value != before;
        }

        edit.Typing = false;
        var face = new NumberFieldFace(width, ui.Theme.IconSize);
        var state = NumberFieldBehavior(ui, fieldId, edit, ref value, min, max, face, enabled);
        var painter = ui.Theme.NumberFieldPainter;
        if (painter != null)
            painter(ui, state, face);
        else
            ui.PaintNumberField(state, face);
        ui.Semantics(fieldId, SemanticRole.Slider, tooltip ?? id, enabled ? SemanticStates.None : SemanticStates.Disabled, state.Text);
        if (tooltip != null)
            ui.Tooltip(fieldId, tooltip);
        return value != before;
    }

    private static NumberFieldState NumberFieldBehavior(Ui ui, int id, NumberFieldEdit edit, ref int value, int min, int max,
        in NumberFieldFace face, bool enabled)
    {
        var text = ui.Memo("number-field-text", value, static v => v.ToString(CultureInfo.InvariantCulture));
        if (!enabled)
        {
            edit.Pressed = NumberFieldPart.None;
            return new NumberFieldState(id, text, false, false, NumberFieldPart.None, NumberFieldPart.None, false, false, value <= min, value >= max);
        }

        var i = ui.Interact(id);
        var focused = ui.TabStop(id);
        var hasRect = ui.TryGetRect(id, out var rect);
        var pointerX = ui.Input.Pointer.X;
        var hovered = i.Hovered && hasRect ? PartAt(rect, face.ArrowWidth, pointerX) : NumberFieldPart.None;
        if (hovered == NumberFieldPart.Value || edit.Scrubbing)
            ui.SetCursor(MouseCursor.ResizeHorizontal);

        if (i.Pressed)
            Press(ui, edit, hovered, pointerX, ref value, min, max);
        else if (i.Held)
            Hold(ui, edit, pointerX, ref value, min, max);
        else if (edit.Pressed != NumberFieldPart.None)
            Release(ui, id, edit, value);

        if (i.Hovered && !i.Held)
            value = Math.Clamp(value + WheelSteps(ui, edit), min, max);
        else
            edit.Wheel = 0f;

        if (focused && !edit.Typing)
        {
            var step = (ui.Input.TakeKey(KeyboardKey.UpArrow) ? 1 : 0) - (ui.Input.TakeKey(KeyboardKey.DownArrow) ? 1 : 0);
            value = Math.Clamp(value + step, min, max);
            if (ui.Input.TakeKey(KeyboardKey.Enter))
                BeginTyping(ui, id, edit, value);
        }

        text = ui.Memo("number-field-text", value, static v => v.ToString(CultureInfo.InvariantCulture));
        var pressed = i.Held ? edit.Pressed : NumberFieldPart.None;
        return new NumberFieldState(id, text, true, i.Hovered || i.Held, hovered, pressed, edit.Scrubbing, focused && ui.FocusVisible,
            value <= min, value >= max);
    }

    private static NumberFieldPart PartAt(UiRect rect, float arrowWidth, float x)
    {
        if (x < rect.X + arrowWidth)
            return NumberFieldPart.Decrease;
        return x >= rect.Right - arrowWidth ? NumberFieldPart.Increase : NumberFieldPart.Value;
    }

    private static void Press(Ui ui, NumberFieldEdit edit, NumberFieldPart part, float x, ref int value, int min, int max)
    {
        edit.Pressed = part;
        edit.PressX = x;
        edit.PressValue = value;
        edit.Scrubbing = false;
        if (part is NumberFieldPart.Decrease or NumberFieldPart.Increase)
        {
            value = Math.Clamp(value + (part == NumberFieldPart.Increase ? 1 : -1), min, max);
            edit.NextRepeat = ui.RealTime + ArrowRepeatDelay;
            ui.RequestFrame();
        }
    }

    private static void Hold(Ui ui, NumberFieldEdit edit, float x, ref int value, int min, int max)
    {
        if (edit.Pressed is NumberFieldPart.Decrease or NumberFieldPart.Increase)
        {
            if (ui.RealTime >= edit.NextRepeat)
            {
                value = Math.Clamp(value + (edit.Pressed == NumberFieldPart.Increase ? 1 : -1), min, max);
                edit.NextRepeat = ui.RealTime + ArrowRepeatInterval;
            }

            ui.RequestFrame();
            return;
        }

        if (edit.Pressed != NumberFieldPart.Value)
            return;
        var travel = x - edit.PressX;
        if (!edit.Scrubbing && MathF.Abs(travel) < ScrubThreshold)
            return;
        edit.Scrubbing = true;
        var steps = (int)MathF.Round(travel / ScrubPixelsPerStep, MidpointRounding.AwayFromZero);
        value = Math.Clamp(edit.PressValue + steps, min, max);
    }

    // A press on the value that was not dragged types over it.
    private static void Release(Ui ui, int id, NumberFieldEdit edit, int value)
    {
        var typing = edit.Pressed == NumberFieldPart.Value && !edit.Scrubbing;
        edit.Pressed = NumberFieldPart.None;
        edit.Scrubbing = false;
        if (typing)
            BeginTyping(ui, id, edit, value);
    }

    private static void BeginTyping(Ui ui, int id, NumberFieldEdit edit, int value)
    {
        edit.Typing = true;
        edit.Text = value.ToString(CultureInfo.InvariantCulture);
        ui.Focus(NumberTextId(ui, id));
        ui.RequestFrame();
    }

    // Whole steps the wheel has turned, up being up; a trackpad's fractions add up across frames.
    private static int WheelSteps(Ui ui, NumberFieldEdit edit)
    {
        edit.Wheel += ui.Input.ScrollY;
        var steps = (int)edit.Wheel;
        edit.Wheel -= steps;
        return steps;
    }

    // The field while it is typed in: a text field of digits, end-aligned short of where the increase arrow was so
    // the number stays where it showed, its whole text selected as it gets the focus. Enter or the focus leaving takes the number typed, Escape drops it; either way the focus goes back to the
    // number field.
    private static int Type(Ui ui, int id, NumberFieldEdit edit, int value, int min, int max, float width)
    {
        var textId = NumberTextId(ui, id);
        if (ui.HasFocus(textId) && ui.Input.TakeKey(KeyboardKey.Escape))
        {
            StopTyping(ui, id, edit, focusBack: true);
            return value;
        }

        if (ui.HasFocus(textId))
        {
            var step = (ui.Input.TakeKey(KeyboardKey.UpArrow) ? 1 : 0) - (ui.Input.TakeKey(KeyboardKey.DownArrow) ? 1 : 0);
            if (step != 0)
            {
                value = Math.Clamp(value + step, min, max);
                edit.Text = value.ToString(CultureInfo.InvariantCulture);
            }
        }

        var text = edit.Text;
        bool entered;
        using (NumberTextScope(ui, id))
            entered = Field(ui, NumberTextName, ref text, null, width, null, null, inline: false, FieldLook.Boxed, clearable: false,
                selectAllOnFocus: true, align: Align.End, paddingEnd: ui.Theme.IconSize);
        edit.Text = OnlyDigits(text, allowMinus: min < 0);
        var left = !ui.HasFocus(textId);
        if (!entered && !left)
            return value;

        if (int.TryParse(edit.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var typed))
            value = Math.Clamp(typed, min, max);
        StopTyping(ui, id, edit, focusBack: entered);
        return value;
    }

    private static void StopTyping(Ui ui, int id, NumberFieldEdit edit, bool focusBack)
    {
        edit.Typing = false;
        if (focusBack)
            ui.Focus(id, takesText: false);
        ui.RequestFrame();
    }

    private static string OnlyDigits(string text, bool allowMinus)
    {
        var clean = true;
        for (var c = 0; c < text.Length; c++)
        {
            if (!char.IsAsciiDigit(text[c]) && !(allowMinus && c == 0 && text[c] == '-'))
                clean = false;
        }

        if (clean)
            return text;
        var kept = new System.Text.StringBuilder(text.Length);
        for (var c = 0; c < text.Length; c++)
        {
            if (char.IsAsciiDigit(text[c]) || (allowMinus && kept.Length == 0 && text[c] == '-'))
                kept.Append(text[c]);
        }
        return kept.ToString();
    }

    /// <summary>The stock look of a <see cref="NumberField"/> not being typed in: a field box
    /// <see cref="UiTheme.ControlHeight"/> tall, the value against its end in tabular figures, short of the increase
    /// arrow's room so it holds still as the arrows show, and the arrows while the field is hovered — for a painter
    /// of an app's own to fall back to.</summary>
    public static void PaintNumberField(this Ui ui, in NumberFieldState state, in NumberFieldFace face)
    {
        var theme = ui.Theme;
        var border = !state.Enabled ? theme.Border : state.Hovered ? theme.BorderStrong : theme.Border;
        using (ui.Row(state.Id, new Box
        {
            Width = face.Width,
            Height = theme.ControlHeight,
            Gap = 0f,
            AlignY = Align.Center,
            Background = state.Enabled ? theme.Field : theme.Control,
            BorderColor = border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        }))
        {
            var arrows = state.Hovered || state.Scrubbing;
            NumberArrow(ui, state, face.ArrowWidth, NumberFieldPart.Decrease, arrows && !state.AtMin);
            var color = state.Enabled ? theme.Text : theme.TextDisabled;
            ui.Text(state.Text, new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }, color, align: Align.End,
                features: FontFeatureSet.TabularFigures);
            NumberArrow(ui, state, face.ArrowWidth, NumberFieldPart.Increase, arrows && !state.AtMax);
            if (state.FocusVisible)
                FocusRing(ui, theme.CornerRadius);
        }
    }

    // An arrow's room at the field's end, the arrow in it when shown: brighter under the pointer, on a pressed fill
    // while held.
    private static void NumberArrow(Ui ui, in NumberFieldState state, float width, NumberFieldPart part, bool shown)
    {
        var theme = ui.Theme;
        var pressed = state.PressedPart == part;
        var fill = shown && pressed ? theme.ControlPressed : 0u;
        using (ui.Box(new Box { Width = width, Height = Sizing.Grow(), Background = fill, Radius = theme.CornerRadius }))
        {
            if (!shown)
                return;
            var ink = state.HoveredPart == part ? theme.Text : theme.TextMuted;
            ui.Paint((ink, Right: part == NumberFieldPart.Increase, theme.BorderWidth), static (painter, arrow) =>
            {
                var r = painter.Rect;
                var middleX = r.X + r.Width / 2f;
                var middleY = r.Y + r.Height / 2f;
                const float half = 3f;
                var tip = arrow.Right ? middleX + half / 2f : middleX - half / 2f;
                var back = arrow.Right ? tip - half : tip + half;
                var thickness = MathF.Max(1.5f, arrow.BorderWidth * 1.5f);
                painter.Line(new PointF(back, middleY - half), new PointF(tip, middleY), thickness, arrow.ink);
                painter.Line(new PointF(tip, middleY), new PointF(back, middleY + half), thickness, arrow.ink);
            });
        }
    }
}
