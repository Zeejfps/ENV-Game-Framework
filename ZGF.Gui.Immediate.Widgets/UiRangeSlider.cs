using System.Globalization;
using ZGF.Desktop;
using ZGF.Fonts;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    // How far in from each end of a range slider's bar a press takes that end rather than the whole bar.
    private const float RangeEdgeGrab = 8f;

    private sealed class RangeDrag
    {
        public RangeSliderPart Part;
        public float PressX;
        public int PressStart;
        public int PressEnd;
    }

    /// <summary>
    /// Edits a range of whole steps, <paramref name="start"/> to <paramref name="end"/> both included, within
    /// <paramref name="min"/> to <paramref name="max"/>, as an animation program's range slider does: a track as wide
    /// as its layout gives it standing for every step, and on it a bar for the range, its start and end numbered inside
    /// it. Dragging the bar slides the range, its length kept; dragging either end of it moves that end, never past
    /// the other; a press on the track outside the bar moves the bar there; a double-click on the bar takes in every
    /// step. Left and Right slide it a step while it has the focus. Returns true on the frame the range changes. It
    /// is drawn by the theme's <see cref="UiTheme.RangeSliderPainter"/>, or as <see cref="PaintRangeSlider"/> draws
    /// one.
    /// </summary>
    public static bool RangeSlider(this Ui ui, string id, ref int start, ref int end, int min, int max, string? tooltip = null,
        bool enabled = true)
    {
        if (max < min)
            throw new ArgumentOutOfRangeException(nameof(max), max, $"The most a range slider takes is less than its least, {min}.");
        var sliderId = ui.GetId(id);
        var (before, beforeEnd) = (start, end);
        start = Math.Clamp(start, min, max);
        end = Math.Clamp(end, start, max);
        var state = RangeSliderBehavior(ui, sliderId, ref start, ref end, min, max, enabled);
        var painter = ui.Theme.RangeSliderPainter;
        if (painter != null)
            painter(ui, state);
        else
            ui.PaintRangeSlider(state);
        var value = ui.Memo("range-slider-value", (start, end), static r => $"{r.start} to {r.end}");
        ui.Semantics(sliderId, SemanticRole.Slider, tooltip ?? id, enabled ? SemanticStates.None : SemanticStates.Disabled, value);
        if (tooltip != null)
            ui.Tooltip(sliderId, tooltip);
        return start != before || end != beforeEnd;
    }

    private static RangeSliderState RangeSliderBehavior(Ui ui, int id, ref int start, ref int end, int min, int max, bool enabled)
    {
        var drag = ui.GetState<RangeDrag>(HashCode.Combine(id, "range-drag"));
        RangeSliderPart hovered;
        var focused = false;
        if (!enabled)
        {
            drag.Part = RangeSliderPart.None;
            hovered = RangeSliderPart.None;
        }
        else
        {
            var i = ui.Interact(id);
            focused = ui.TabStop(id);
            var hasRect = ui.TryGetRect(id, out var rect);
            var x = ui.Input.Pointer.X;
            hovered = i.Hovered && hasRect ? RangePartAt(rect, x, start, end, min, max) : RangeSliderPart.None;
            var resizing = hovered is RangeSliderPart.Start or RangeSliderPart.End || drag.Part is RangeSliderPart.Start or RangeSliderPart.End;
            if (resizing)
                ui.SetCursor(MouseCursor.ResizeHorizontal);

            if (i.Pressed && hasRect)
                PressRange(drag, hovered, x, rect, ref start, ref end, min, max, i.DoubleClicked);
            else if (i.Held && hasRect)
                DragRange(drag, x, rect, ref start, ref end, min, max);
            else
                drag.Part = RangeSliderPart.None;

            if (focused)
            {
                var step = (ui.Input.TakeKey(KeyboardKey.RightArrow) ? 1 : 0) - (ui.Input.TakeKey(KeyboardKey.LeftArrow) ? 1 : 0);
                SlideRange(ref start, ref end, step, min, max);
            }
        }

        var startText = ui.Memo("range-slider-start", start, static v => v.ToString(CultureInfo.InvariantCulture));
        var endText = ui.Memo("range-slider-end", end, static v => v.ToString(CultureInfo.InvariantCulture));
        return new RangeSliderState(id, min, max, start, end, startText, endText, enabled, hovered, drag.Part, focused && ui.FocusVisible);
    }

    // The part of the slider at x: an end of the bar within the grab of its edge (the nearer one when the bar is too
    // narrow for both), the bar between, or the track.
    private static RangeSliderPart RangePartAt(UiRect rect, float x, int start, int end, int min, int max)
    {
        var steps = max - min + 1f;
        var left = rect.X + rect.Width * (start - min) / steps;
        var right = rect.X + rect.Width * (end - min + 1f) / steps;
        if (x < left || x >= right)
            return RangeSliderPart.Track;
        var grab = MathF.Min(RangeEdgeGrab, (right - left) / 3f);
        if (x < left + grab)
            return RangeSliderPart.Start;
        return x >= right - grab ? RangeSliderPart.End : RangeSliderPart.Bar;
    }

    private static void PressRange(RangeDrag drag, RangeSliderPart part, float x, UiRect rect, ref int start, ref int end, int min, int max,
        bool doubleClicked)
    {
        if (part == RangeSliderPart.Bar && doubleClicked)
        {
            (start, end) = (min, max);
            drag.Part = RangeSliderPart.None;
            return;
        }

        if (part == RangeSliderPart.Track)
        {
            // The bar moves to center on the step pressed, then slides with the pointer.
            var steps = max - min + 1f;
            var pressed = min + (int)MathF.Floor((x - rect.X) / rect.Width * steps);
            var length = end - start;
            SlideRange(ref start, ref end, pressed - length / 2 - start, min, max);
            part = RangeSliderPart.Bar;
        }

        drag.Part = part;
        drag.PressX = x;
        drag.PressStart = start;
        drag.PressEnd = end;
    }

    private static void DragRange(RangeDrag drag, float x, UiRect rect, ref int start, ref int end, int min, int max)
    {
        if (drag.Part == RangeSliderPart.None || rect.Width <= 0f)
            return;
        var stepWidth = rect.Width / (max - min + 1f);
        var steps = (int)MathF.Round((x - drag.PressX) / stepWidth, MidpointRounding.AwayFromZero);
        switch (drag.Part)
        {
            case RangeSliderPart.Start:
                start = Math.Clamp(drag.PressStart + steps, min, end);
                break;
            case RangeSliderPart.End:
                end = Math.Clamp(drag.PressEnd + steps, start, max);
                break;
            case RangeSliderPart.Bar:
                (start, end) = (drag.PressStart, drag.PressEnd);
                SlideRange(ref start, ref end, steps, min, max);
                break;
        }
    }

    // Moves the whole range by steps, its length kept, stopping at either end of the track.
    private static void SlideRange(ref int start, ref int end, int steps, int min, int max)
    {
        var shift = Math.Clamp(steps, min - start, max - end);
        start += shift;
        end += shift;
    }

    /// <summary>The stock look of a <see cref="RangeSlider"/>: a recessed track <see cref="UiTheme.ControlHeight"/>
    /// tall, and on it the bar raised on the control fill, a grip at each end and its start and end numbered inside
    /// it in tabular figures when it is wide enough; the part under the pointer brighter, the part dragged pressed
    /// in — for a painter of an app's own to fall back to.</summary>
    public static void PaintRangeSlider(this Ui ui, in RangeSliderState state)
    {
        var theme = ui.Theme;
        using (ui.Box(state.Id, new Box
        {
            Width = Sizing.Grow(),
            Height = theme.ControlHeight,
            Background = theme.Field,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        }))
        {
            if (state.FocusVisible)
                FocusRing(ui, theme.CornerRadius);
            var barActive = state.PressedPart == RangeSliderPart.Bar || (state.PressedPart == RangeSliderPart.None && state.HoveredPart == RangeSliderPart.Bar);
            var look = new RangeBarLook
            {
                Fractions = state.BarFractions,
                Fill = !state.Enabled ? theme.Control : state.PressedPart == RangeSliderPart.Bar ? theme.ControlPressed
                    : barActive ? theme.ControlHovered : theme.Control,
                Outline = state.Enabled ? theme.BorderStrong : theme.Border,
                StartGrip = GripInk(theme, state, RangeSliderPart.Start),
                EndGrip = GripInk(theme, state, RangeSliderPart.End),
                Text = state.Enabled ? theme.Text : theme.TextDisabled,
                StartText = state.StartText,
                EndText = state.EndText,
                TextWidth = ui.MeasureText(state.StartText, theme.FontSizeSmall, features: FontFeatureSet.TabularFigures)
                    + ui.MeasureText(state.EndText, theme.FontSizeSmall, features: FontFeatureSet.TabularFigures),
                Inset = theme.BorderWidth * 2f,
                Radius = theme.RadiusSmall,
                FontSize = theme.FontSizeSmall,
                BorderWidth = theme.BorderWidth,
            };
            ui.Paint(look, static (painter, bar) => PaintRangeBar(painter, bar));
        }
    }

    private static uint GripInk(UiTheme theme, in RangeSliderState state, RangeSliderPart end)
    {
        if (!state.Enabled)
            return theme.TextDisabled;
        return state.PressedPart == end || (state.PressedPart == RangeSliderPart.None && state.HoveredPart == end) ? theme.Text : theme.TextMuted;
    }

    private static void PaintRangeBar(UiPainter painter, RangeBarLook bar)
    {
        var r = painter.Rect;
        var inner = new UiRect(r.X + bar.Inset, r.Y + bar.Inset, r.Width - bar.Inset * 2f, r.Height - bar.Inset * 2f);
        var left = inner.X + inner.Width * bar.Fractions.Left;
        var right = inner.X + inner.Width * bar.Fractions.Right;
        var body = new UiRect(left, inner.Y, MathF.Max(2f, right - left), inner.Height);
        painter.FillRect(body, bar.Fill, bar.Radius, bar.Outline, bar.BorderWidth);

        // A grip at each end: two short lines a step in from the edge, brighter while the end is hovered or dragged.
        const float gripHeight = 8f;
        var gripTop = body.Y + (body.Height - gripHeight) / 2f;
        if (body.Width >= 16f)
        {
            Grip(painter, body.X + 4f, gripTop, gripHeight, bar.StartGrip);
            Grip(painter, body.Right - 7f, gripTop, gripHeight, bar.EndGrip);
        }

        // The numbers between the grips, when they fit.
        var room = body.Width - 24f;
        if (room < bar.TextWidth + 8f)
            return;
        var labels = new UiRect(body.X + 12f, body.Y, body.Width - 24f, body.Height);
        painter.Text(labels, bar.StartText, bar.Text, bar.FontSize, align: Align.Start, features: FontFeatureSet.TabularFigures);
        painter.Text(labels, bar.EndText, bar.Text, bar.FontSize, align: Align.End, features: FontFeatureSet.TabularFigures);
    }

    private static void Grip(UiPainter painter, float x, float top, float height, uint ink)
    {
        painter.FillRect(new UiRect(x, top, 1f, height), ink);
        painter.FillRect(new UiRect(x + 2f, top, 1f, height), ink);
    }

    private readonly record struct RangeBarLook
    {
        public required (float Left, float Right) Fractions { get; init; }
        public required uint Fill { get; init; }
        public required uint Outline { get; init; }
        public required uint StartGrip { get; init; }
        public required uint EndGrip { get; init; }
        public required uint Text { get; init; }
        public required string StartText { get; init; }
        public required string EndText { get; init; }
        public required float TextWidth { get; init; }
        public required float Inset { get; init; }
        public required float Radius { get; init; }
        public required float FontSize { get; init; }
        public required float BorderWidth { get; init; }
    }
}
