using ZGF.Desktop;
using ZGF.Desktop.Input;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const string AreaScrollSalt = "scroll";

    /// <summary>How a text area's keys edit it: the width its lines wrap at, and whether plain Enter
    /// submits rather than breaking the line.</summary>
    private readonly record struct AreaEditing(float Width, bool SubmitOnEnter);

    /// <summary>What a text area paints under its text: <see cref="Shown"/> is the text with any
    /// composition in it, which starts at <see cref="PreeditStart"/>; the selection and caret index into it.</summary>
    private readonly record struct AreaPaint(Ui Ui, string Shown, int SelectionStart, int SelectionEnd, int Caret, bool ShowsCaret,
        PreeditText Preedit, int PreeditStart);

    /// <summary>Edits <paramref name="text"/> in place across lines that wrap between words to fit the
    /// field's width. The field grows with its text from <paramref name="minLines"/> to
    /// <paramref name="maxLines"/> lines tall, then scrolls, keeping the caret in view. Returns true
    /// when the text is submitted: by Enter when <paramref name="submitOnEnter"/>, Shift+Enter then
    /// breaking the line, else by Ctrl/Cmd+Enter, Enter breaking it; Ctrl/Cmd+Enter submits either way.
    /// Up and Down move between lines, keeping the caret's x; Home and End go to the ends of its line,
    /// and with Ctrl/Cmd to the ends of the text. Otherwise editing works as in <see cref="TextField"/>,
    /// except that pasted line breaks are kept. When not <paramref name="enabled"/> it shows
    /// <paramref name="text"/> greyed out, the same size and scrolled as far, and ignores input but the wheel.</summary>
    public static bool TextArea(this Ui ui, string id, ref string text, string? placeholder = null, Sizing? width = null, int minLines = 1,
        int maxLines = 8, bool submitOnEnter = true, bool enabled = true)
    {
        if (!enabled)
        {
            DisabledArea(ui, ui.GetId(id), text, placeholder, width, minLines, maxLines);
            return false;
        }
        var theme = ui.Theme;
        var field = ui.Interact(id);
        var fieldId = field.Id;
        var scrollId = HashCode.Combine(fieldId, AreaScrollSalt);
        // The scroll holding the lines is hit before the field around it, so a press on either is
        // the field's; the one pressed follows the pointer until it is released.
        var lines = ui.Interact(scrollId);
        var i = lines.Pressed || lines.Held ? lines : field;
        var hovered = field.Hovered || lines.Hovered;
        if (hovered || i.Held) ui.SetCursor(MouseCursor.Text);

        var state = ui.GetState<TextFieldState>(fieldId);
        Adopt(state, text);

        var focused = ui.TabStop(fieldId, takesText: true);
        var (caretBefore, anchorBefore, textBefore) = (state.Caret, state.Anchor, text);
        var hasRect = TryGetLinesRect(ui, scrollId, out var content);
        var area = new AreaEditing(hasRect ? content.Width : float.PositiveInfinity, submitOnEnter);
        var lineHeight = ui.LineHeight();
        var preedit = focused ? ui.Input.Composition : PreeditText.Empty;

        if (i.Pressed && !preedit.IsEmpty)
        {
            ui.ResetComposition();
            preedit = PreeditText.Empty;
        }
        if (hasRect && (i.Pressed || (i.Held && focused)))
        {
            var pointer = ui.Input.Pointer;
            var at = AreaCaretAt(ui, text, area.Width, pointer.X - content.X, pointer.Y - content.Y, lineHeight);
            SelectWithPointer(ui, i, state.Selection, text, at, focused && IsShiftHeld(ui), content.X, content.Right);
            state.LastEdit = EditKind.None;
            state.GoalX = -1f;
            // Dragging above or below the field scrolls it, a frame at a time, as the caret follows.
            if (i.Held && ui.TryGetRect(scrollId, out var view) && (pointer.Y < view.Y || pointer.Y > view.Bottom))
                ui.RequestUpdate();
        }
        if (i.Pressed)
        {
            ui.Focus(fieldId);
            focused = true;
        }

        var submitted = false;
        if (focused)
        {
            if (!preedit.IsEmpty && state.HasSelection)
                Replace(state, ref text, state.SelectionStart, state.SelectionEnd, "", EditKind.None);
            submitted = Edit(ui, state, ref text, area);
            focused = ui.HasFocus(fieldId);
        }
        if (!focused)
            preedit = PreeditText.Empty;

        var shown = preedit.IsEmpty ? text : text.Insert(state.Caret, preedit.Text);
        var shownCaret = state.Caret + preedit.Caret;
        if (focused)
        {
            var moved = state.Caret != caretBefore || state.Anchor != anchorBefore || !ReferenceEquals(text, textBefore) || i.Pressed;
            if (moved)
                state.BlinkFrom = ui.RealTime;
            if (hasRect)
            {
                var shownLines = ui.BreakText(shown, area.Width);
                var caretLine = shownLines.LineOf(shownCaret);
                var caretX = XInLine(ui, shown, shownLines, caretLine, shownCaret);
                var caretY = caretLine * lineHeight;
                ui.SetImeCaret(new UiRect(content.X + caretX, content.Y + caretY, theme.CaretWidth, lineHeight));
                if (moved || !preedit.IsEmpty)
                    KeepInView(ui, scrollId, caretY, lineHeight);
            }
        }

        var selected = focused && state.HasSelection;
        var paint = new AreaPaint(ui, shown, selected ? state.SelectionStart : 0, selected ? state.SelectionEnd : 0, shownCaret,
            focused && !state.HasSelection && CaretVisible(ui, state), preedit, state.Caret);

        // Padded so that one line stands as tall as a TextField.
        var paddingY = MathF.Max(0f, (theme.ControlHeight - lineHeight) / 2);
        using (ui.Column(fieldId, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Padding = new Spacing(theme.FieldPaddingX, paddingY),
            Gap = 0f,
            Background = theme.Field,
            BorderColor = FieldBorder(theme, FieldLook.Boxed, focused, hovered),
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        }))
        using (ui.Scroll(scrollId, ScrollAxes.Vertical, new Box { Height = Sizing.Fit(minLines * lineHeight, maxLines * lineHeight), Gap = 0f },
            showBars: false))
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
        {
            ui.Paint(paint, static (painter, p) => PaintArea(painter, p));
            if (shown.Length > 0)
                ui.Text(shown, wrap: true);
            else if (placeholder != null)
                ui.Text(placeholder, color: theme.TextMuted, wrap: true);
        }
        return submitted;
    }

    /// <summary>A text area's look, as <see cref="TextArea"/> lays it out, on the control color with its text in
    /// <see cref="UiTheme.TextDisabled"/>, taking no input but the wheel.</summary>
    private static void DisabledArea(Ui ui, int fieldId, string text, string? placeholder, Sizing? width, int minLines, int maxLines)
    {
        var theme = ui.Theme;
        var lineHeight = ui.LineHeight();
        var paddingY = MathF.Max(0f, (theme.ControlHeight - lineHeight) / 2);
        using (ui.Column(fieldId, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Padding = new Spacing(theme.FieldPaddingX, paddingY),
            Gap = 0f,
            Background = theme.Control,
            BorderColor = theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = theme.CornerRadius,
        }))
        using (ui.Scroll(HashCode.Combine(fieldId, AreaScrollSalt), ScrollAxes.Vertical,
            new Box { Height = Sizing.Fit(minLines * lineHeight, maxLines * lineHeight), Gap = 0f }, showBars: false))
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = 0f }))
            ui.Text(text.Length > 0 ? text : placeholder ?? "", color: theme.TextDisabled, wrap: true);
    }

    /// <summary>Where the area's lines were laid out on the previous frame: as wide as the scroll that
    /// holds them, from its top less how far it is scrolled.</summary>
    private static bool TryGetLinesRect(Ui ui, int scrollId, out UiRect rect)
    {
        if (!ui.TryGetRect(scrollId, out var view))
        {
            rect = default;
            return false;
        }
        var offset = ui.ScrollOffset(scrollId).Y;
        rect = new UiRect(view.X, view.Y - offset, view.Width, view.Height + offset);
        return true;
    }

    /// <summary>Scrolls the area's lines the least distance that shows the line at
    /// <paramref name="top"/> whole.</summary>
    private static void KeepInView(Ui ui, int scrollId, float top, float lineHeight)
    {
        if (!ui.TryGetRect(scrollId, out var view))
            return;
        var offset = ui.ScrollOffset(scrollId).Y;
        if (top < offset)
            ui.ScrollTo(scrollId, 0f, top);
        else if (top + lineHeight > offset + view.Height)
            ui.ScrollTo(scrollId, 0f, top + lineHeight - view.Height);
    }

    /// <summary>The selection, the composition's underlines and the caret, laid out on the lines the
    /// text is drawn on: broken at the width the content has this frame.</summary>
    private static void PaintArea(UiPainter painter, AreaPaint p)
    {
        var ui = p.Ui;
        var theme = ui.Theme;
        var lines = ui.BreakText(p.Shown, painter.Rect.Width);
        var lineHeight = ui.LineHeight();
        if (p.SelectionStart < p.SelectionEnd)
            PaintRange(painter, ui, p.Shown, lines, p.SelectionStart, p.SelectionEnd, theme.TextSelection, 0f, lineHeight, pastLineEnd: true);

        var blocks = p.Preedit.Blocks;
        if (!p.Preedit.IsEmpty && blocks.Count == 0)
            Underline(painter, ui, p.Shown, lines, p.PreeditStart, p.PreeditStart + p.Preedit.Text.Length, lineHeight, theme.BorderWidth);
        for (var b = 0; b < blocks.Count; b++)
        {
            var from = p.PreeditStart + blocks[b].Start;
            Underline(painter, ui, p.Shown, lines, from, from + blocks[b].Length, lineHeight,
                b == p.Preedit.FocusedBlock ? theme.FocusRingWidth : theme.BorderWidth);
        }

        if (!p.ShowsCaret)
            return;
        var line = lines.LineOf(p.Caret);
        var x = XInLine(ui, p.Shown, lines, line, p.Caret);
        painter.FillRect(new UiRect(painter.Rect.X + x, painter.Rect.Y + line * lineHeight, theme.CaretWidth, lineHeight), theme.Text);
    }

    private static void Underline(UiPainter painter, Ui ui, string text, TextLines lines, int from, int to, float lineHeight, float thickness) =>
        PaintRange(painter, ui, text, lines, from, to, ui.Theme.Text, lineHeight - thickness, thickness, pastLineEnd: false);

    /// <summary>Fills a band <paramref name="height"/> tall, <paramref name="drop"/> below the top of
    /// each line, under the part of [<paramref name="from"/>, <paramref name="to"/>) on that line. With
    /// <paramref name="pastLineEnd"/>, a range going on past a line's end covers a space beyond it, so
    /// a selected line break shows.</summary>
    private static void PaintRange(UiPainter painter, Ui ui, string text, TextLines lines, int from, int to, uint color, float drop, float height,
        bool pastLineEnd)
    {
        var rect = painter.Rect;
        var lineHeight = ui.LineHeight();
        var newline = pastLineEnd ? ui.MeasureText(" ") : 0f;
        var last = lines.LineOf(to);
        for (var line = lines.LineOf(from); line <= last; line++)
        {
            var start = lines.Start(line);
            var end = start + lines.Length(line);
            var left = XInLine(ui, text, lines, line, Math.Clamp(from, start, end));
            var right = XInLine(ui, text, lines, line, Math.Clamp(to, start, end));
            if (to > end)
                right += newline;
            if (right > left)
                painter.FillRect(new UiRect(rect.X + left, rect.Y + line * lineHeight + drop, right - left, height), color);
        }
    }

    /// <summary>The caret index nearest the point (<paramref name="x"/>, <paramref name="y"/>) from the
    /// top-left of <paramref name="text"/> broken into lines <paramref name="width"/> wide.</summary>
    private static int AreaCaretAt(Ui ui, string text, float width, float x, float y, float lineHeight)
    {
        var lines = ui.BreakText(text, width);
        var line = Math.Clamp((int)MathF.Floor(y / lineHeight), 0, lines.Count - 1);
        return CaretInLine(ui, text, lines, line, x);
    }

    /// <summary>Moves the caret a line up or down, to the index nearest the x it had before the first
    /// of a run of such moves; past the first or last line, to the start or end of the text.</summary>
    private static void MoveLine(Ui ui, TextFieldState state, string text, AreaEditing area, int direction, bool extend)
    {
        var lines = ui.BreakText(text, area.Width);
        var line = lines.LineOf(state.Caret);
        var goalX = state.GoalX >= 0f ? state.GoalX : XInLine(ui, text, lines, line, state.Caret);
        var target = line + direction;
        var caret = target < 0 ? 0 : target >= lines.Count ? text.Length : CaretInLine(ui, text, lines, target, goalX);
        Move(state, caret, extend);
        state.GoalX = goalX;
    }

    private static int LineStart(Ui ui, string text, AreaEditing area, int index)
    {
        var lines = ui.BreakText(text, area.Width);
        return lines.Start(lines.LineOf(index));
    }

    private static int LineEnd(Ui ui, string text, AreaEditing area, int index)
    {
        var lines = ui.BreakText(text, area.Width);
        var line = lines.LineOf(index);
        return lines.Start(line) + lines.Length(line);
    }

    /// <summary>The x of <paramref name="index"/> from the start of its <paramref name="line"/>, the
    /// line's text measured on its own as it is drawn.</summary>
    private static float XInLine(Ui ui, string text, TextLines lines, int line, int index)
    {
        var start = lines.Start(line);
        return ui.MeasureText(text.AsSpan(start, index - start));
    }

    /// <summary>The caret index on <paramref name="line"/> nearest <paramref name="x"/> from its start,
    /// on a grapheme-cluster boundary, as <see cref="CaretAt"/> finds it in a single line.</summary>
    private static int CaretInLine(Ui ui, string text, TextLines lines, int line, float x)
    {
        var start = lines.Start(line);
        var end = start + lines.Length(line);
        var low = start;
        var high = end;
        while (low < high)
        {
            var middle = (low + high + 1) / 2;
            if (XInLine(ui, text, lines, line, middle) <= x)
                low = middle;
            else
                high = middle - 1;
        }
        low = TextBoundaries.Snap(text, low);
        if (low >= end)
            return end;
        var next = Math.Min(TextBoundaries.Next(text, low), end);
        var pastLow = x - XInLine(ui, text, lines, line, low);
        var beforeNext = XInLine(ui, text, lines, line, next) - x;
        return beforeNext < pastLow ? next : low;
    }
}
