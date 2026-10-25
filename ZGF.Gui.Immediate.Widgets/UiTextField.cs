using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Where an <see cref="UiWidgets.InlineEdit"/> stands after this frame.</summary>
public enum InlineEditResult
{
    Editing,

    /// <summary>Enter was pressed, or focus moved elsewhere: keep the text.</summary>
    Committed,

    /// <summary>Escape was pressed: drop the text.</summary>
    Cancelled,
}

/// <summary>How a <see cref="UiWidgets.TextField"/> looks while it is not being edited.</summary>
public enum FieldLook
{
    /// <summary>A box on the field color, always.</summary>
    Boxed,

    /// <summary>Plain text, as a value in a table's cell: the outline shows while the pointer is over
    /// it, and the box while it has focus.</summary>
    Cell,
}

public static partial class UiWidgets
{
    private const double CaretBlinkSeconds = 0.53;
    private const int MaxUndo = 100;

    private enum EditKind
    {
        None,
        Typing,
        Deleting,
    }

    private readonly record struct TextSnapshot(string Text, int Caret, int Anchor);

    private sealed class TextFieldState
    {
        public readonly TextSelection Selection = new();
        public float Shift;
        public string Known = "";
        public EditKind LastEdit;
        public readonly List<TextSnapshot> Undo = new();
        public readonly List<TextSnapshot> Redo = new();
        public double BlinkFrom;

        public int Caret => Selection.Caret;
        public int Anchor => Selection.Anchor;
        public bool HasSelection => Selection.HasSelection;
        public int SelectionStart => Selection.Start;
        public int SelectionEnd => Selection.End;

        public void Select(int anchor, int caret)
        {
            Selection.Select(anchor, caret);
            LastEdit = EditKind.None;
        }
    }

    private static uint FieldBackground(UiTheme theme, FieldLook look, bool focused)
    {
        if (look == FieldLook.Cell && !focused)
            return 0u;
        return theme.Field;
    }

    /// <summary>The field's outline: the focus color while focused, stronger while hovered, and for a
    /// cell none otherwise. The width stays, so the text does not move.</summary>
    private static uint FieldBorder(UiTheme theme, FieldLook look, bool focused, bool hovered)
    {
        if (focused)
            return theme.FocusRing;
        if (hovered)
            return theme.BorderStrong;
        if (look == FieldLook.Cell)
            return 0u;
        return theme.Border;
    }

    /// <summary>Edits <paramref name="text"/> in place. Returns true when Enter is pressed while
    /// the field has focus. Supports mouse selection (drag, shift-click, double-click for a word,
    /// triple-click for everything), word-wise movement and deletion, the clipboard, and undo. The
    /// field is a Tab stop. <paramref name="leadingIcon"/> is a glyph drawn before the text;
    /// <paramref name="hint"/> is muted text at the end — a shortcut —
    /// left out while the field is too narrow to keep room for typing. <paramref name="look"/> says
    /// how it looks while not being edited.</summary>
    public static bool TextField(this Ui ui, string id, ref string text, string? placeholder = null, Sizing? width = null, IconGlyph? leadingIcon = null,
        string? hint = null, FieldLook look = FieldLook.Boxed) =>
        Field(ui, id, ref text, placeholder, width, leadingIcon, hint, inline: false, look);

    /// <summary>Edits <paramref name="text"/> in place of the label it replaces, as in a rename: a
    /// field one line tall, its text where the label's was, outlined in the focus color. On the frame
    /// <paramref name="begin"/> is true it takes focus with the whole text selected. Enter, or focus
    /// moving elsewhere, commits; Escape cancels. Editing works as in <see cref="TextField"/>.</summary>
    public static InlineEditResult InlineEdit(this Ui ui, string id, ref string text, bool begin = false, Sizing? width = null)
    {
        var fieldId = ui.GetId(id);
        if (begin)
        {
            var state = ui.GetState<TextFieldState>(fieldId);
            state.Known = text;
            state.Undo.Clear();
            state.Redo.Clear();
            state.Shift = 0f;
            state.Select(0, text.Length);
            ui.Focus(fieldId);
        }
        else if (!ui.HasFocus(fieldId))
        {
            return InlineEditResult.Committed;
        }
        if (ui.Input.TakeKey(KeyboardKey.Escape))
            return InlineEditResult.Cancelled;
        return Field(ui, id, ref text, null, width, null, null, inline: true, FieldLook.Boxed) ? InlineEditResult.Committed : InlineEditResult.Editing;
    }

    private static bool Field(Ui ui, string id, ref string text, string? placeholder, Sizing? width, IconGlyph? leadingIcon, string? hint,
        bool inline, FieldLook look)
    {
        var theme = ui.Theme;
        var padding = inline ? theme.InlineEditPaddingX : theme.FieldPaddingX;
        var i = ui.Interact(id);
        if (i.Hovered || i.Held) ui.SetCursor(MouseCursor.Text);

        var state = ui.GetState<TextFieldState>(i.Id);
        if (!ReferenceEquals(text, state.Known) && text != state.Known)
        {
            state.Known = text;
            state.Undo.Clear();
            state.Redo.Clear();
            state.Select(Math.Min(state.Anchor, text.Length), Math.Min(state.Caret, text.Length));
        }

        var focused = ui.TabStop(i.Id, takesText: true);
        var (caretBefore, anchorBefore, textBefore) = (state.Caret, state.Anchor, text);
        var hasRect = ui.TryGetRect(i.Id, out var rect);
        var lead = leadingIcon != null ? theme.IconSize + theme.GapTight : 0f;
        var hintWidth = hint != null ? ui.MeasureText(hint, theme.FontSizeSmall) + theme.GapTight : 0f;
        var showHint = hint != null && (!hasRect || rect.Width - padding * 2 - lead - hintWidth >= MinFieldWidth);
        var trail = showHint ? hintWidth : 0f;
        var textLeft = rect.X + padding + lead;
        var innerWidth = hasRect ? rect.Width - padding * 2 - lead - trail : float.PositiveInfinity;
        var preedit = focused ? ui.Input.Composition : PreeditText.Empty;

        if (i.Pressed)
        {
            if (!preedit.IsEmpty)
            {
                ui.ResetComposition();
                preedit = PreeditText.Empty;
            }
            if (!focused) state.Shift = 0f;
        }
        var pointing = i.Pressed || (i.Held && focused && hasRect);
        if (pointing)
        {
            var at = ui.CaretAt(text, ui.Input.Pointer.X - textLeft + state.Shift);
            SelectWithPointer(ui, i, state.Selection, text, at, focused && IsShiftHeld(ui), textLeft, textLeft + innerWidth);
            state.LastEdit = EditKind.None;
        }
        if (i.Pressed)
        {
            ui.Focus(i.Id);
            focused = true;
        }

        var submitted = false;
        if (focused)
        {
            if (!preedit.IsEmpty && state.HasSelection)
                Replace(state, ref text, state.SelectionStart, state.SelectionEnd, "", EditKind.None);
            submitted = Edit(ui, state, ref text);
            focused = ui.HasFocus(i.Id);
        }
        if (!focused)
            preedit = PreeditText.Empty;

        var shown = preedit.IsEmpty ? text : text.Insert(state.Caret, preedit.Text);
        var caretX = 0f;
        var anchorX = 0f;
        var lineHeight = ui.LineHeight();
        if (focused)
        {
            caretX = ui.MeasureText(shown, state.Caret + preedit.Caret);
            anchorX = state.HasSelection ? ui.MeasureText(text, state.Anchor) : caretX;
            state.Shift = FollowCaret(state.Shift, caretX, innerWidth, ui.MeasureText(shown));

            if (state.Caret != caretBefore || state.Anchor != anchorBefore || !ReferenceEquals(text, textBefore) || i.Pressed)
                state.BlinkFrom = ui.RealTime;
            if (hasRect)
                ui.SetImeCaret(new UiRect(textLeft + caretX - state.Shift, rect.Y + (rect.Height - lineHeight) / 2, theme.CaretWidth, lineHeight));
        }
        var shift = focused ? state.Shift : 0f;

        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = inline ? lineHeight + theme.InlineEditPaddingY * 2 : theme.ControlHeight,
            Padding = new Spacing(padding, 0f),
            Gap = theme.GapTight,
            Background = FieldBackground(theme, look, focused),
            BorderColor = FieldBorder(theme, look, focused, i.Hovered),
            BorderWidth = theme.BorderWidth,
            Radius = inline ? theme.RadiusSmall : theme.CornerRadius,
        }))
        {
            if (leadingIcon != null)
                ui.Glyph(leadingIcon.Value, theme.TextMuted, theme.IconSize, new Box { Width = theme.IconSize });

            using (ui.Row(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Clip = true, ScrollX = shift }))
            {
                if (focused && state.HasSelection)
                {
                    var from = MathF.Min(caretX, anchorX) - shift;
                    var to = MathF.Max(caretX, anchorX) - shift;
                    PaintSpan(ui, from, to, theme.TextSelection, lineHeight);
                }

                if (!preedit.IsEmpty)
                    UnderlineComposition(ui, shown, state.Caret, preedit, shift, lineHeight);

                if (shown.Length > 0)
                    ui.Text(shown);
                else if (placeholder != null)
                    ui.Text(placeholder, color: theme.TextMuted);

                if (focused && !state.HasSelection && CaretVisible(ui, state))
                {
                    using (ui.Float(0f, 0.5f, caretX - shift, -lineHeight / 2,
                        new Box { Width = theme.CaretWidth, Height = lineHeight, Background = theme.Text }, z: 1)) { }
                }
            }

            if (showHint)
                ui.Text(hint!, color: theme.TextMuted, size: theme.FontSizeSmall);
        }
        return submitted;
    }

    private static void UnderlineComposition(Ui ui, string shown, int start, PreeditText preedit, float shift, float lineHeight)
    {
        var theme = ui.Theme;
        var blocks = preedit.Blocks;
        if (blocks.Count == 0)
        {
            Underline(ui, shown, start, start + preedit.Text.Length, shift, lineHeight, theme.BorderWidth);
            return;
        }
        for (var b = 0; b < blocks.Count; b++)
        {
            var from = start + blocks[b].Start;
            Underline(ui, shown, from, from + blocks[b].Length, shift, lineHeight,
                b == preedit.FocusedBlock ? theme.FocusRingWidth : theme.BorderWidth);
        }
    }

    private static void Underline(Ui ui, string shown, int from, int to, float shift, float lineHeight, float thickness)
    {
        var left = ui.MeasureText(shown, from) - shift;
        var right = ui.MeasureText(shown, to) - shift;
        PaintSpan(ui, left, right, ui.Theme.Text, thickness, (lineHeight - thickness) / 2);
    }

    private static bool CaretVisible(Ui ui, TextFieldState state)
    {
        var phase = (ui.RealTime - state.BlinkFrom) % (CaretBlinkSeconds * 2);
        ui.RequestUpdateIn(CaretBlinkSeconds - phase % CaretBlinkSeconds);
        return phase < CaretBlinkSeconds;
    }

    private static bool Edit(Ui ui, TextFieldState state, ref string text)
    {
        foreach (var stroke in ui.Input.Keys)
        {
            var shortcut = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            var byWord = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) != 0;
            var extend = (stroke.Modifiers & KeyModifiers.Shift) != 0;
            var caret = state.Caret;
            var collapses = !extend && !byWord && state.HasSelection;
            switch (stroke.Key)
            {
                case KeyboardKey.LeftArrow when collapses:
                    Move(state, state.SelectionStart, extend);
                    break;
                case KeyboardKey.LeftArrow:
                    Move(state, StepLeft(text, caret, byWord), extend);
                    break;
                case KeyboardKey.RightArrow when collapses:
                    Move(state, state.SelectionEnd, extend);
                    break;
                case KeyboardKey.RightArrow:
                    Move(state, StepRight(text, caret, byWord), extend);
                    break;
                case KeyboardKey.Home:
                    Move(state, 0, extend);
                    break;
                case KeyboardKey.End:
                    Move(state, text.Length, extend);
                    break;
                case KeyboardKey.Backspace when state.HasSelection:
                case KeyboardKey.Delete when state.HasSelection:
                    Replace(state, ref text, state.SelectionStart, state.SelectionEnd, "", EditKind.None);
                    break;
                case KeyboardKey.Backspace when caret > 0:
                    Replace(state, ref text, StepLeft(text, caret, byWord), caret, "", EditKind.Deleting);
                    break;
                case KeyboardKey.Delete when caret < text.Length:
                    Replace(state, ref text, caret, StepRight(text, caret, byWord), "", EditKind.Deleting);
                    break;
                case KeyboardKey.A when shortcut:
                    state.Select(0, text.Length);
                    break;
                case KeyboardKey.C when shortcut && state.HasSelection:
                    ui.ClipboardText = state.Selection.Of(text);
                    break;
                case KeyboardKey.X when shortcut && state.HasSelection:
                    ui.ClipboardText = state.Selection.Of(text);
                    Replace(state, ref text, state.SelectionStart, state.SelectionEnd, "", EditKind.None);
                    break;
                case KeyboardKey.V when shortcut:
                    var pasted = ui.ClipboardText.ReplaceLineEndings(" ");
                    if (pasted.Length > 0)
                        Replace(state, ref text, state.SelectionStart, state.SelectionEnd, pasted, EditKind.None);
                    break;
                case KeyboardKey.Z when shortcut && extend:
                case KeyboardKey.Y when shortcut:
                    Restore(state, ref text, state.Redo, state.Undo);
                    break;
                case KeyboardKey.Z when shortcut:
                    Restore(state, ref text, state.Undo, state.Redo);
                    break;
                case KeyboardKey.Enter:
                    return true;
                case KeyboardKey.Escape:
                    ui.ClearFocus();
                    return false;
            }
        }

        if (ui.Input.Text.Length > 0)
            Replace(state, ref text, state.SelectionStart, state.SelectionEnd, ui.Input.Text, EditKind.Typing);
        return false;
    }

    private static void Move(TextFieldState state, int caret, bool extend) =>
        state.Select(extend ? state.Anchor : caret, caret);

    // Runs of typing, or of deleting, merge into one undo step until the caret moves or the kind
    // of edit changes.
    private static void Replace(TextFieldState state, ref string text, int start, int end, string insert, EditKind kind)
    {
        if (kind == EditKind.None || kind != state.LastEdit)
        {
            if (state.Undo.Count == MaxUndo) state.Undo.RemoveAt(0);
            state.Undo.Add(new TextSnapshot(text, state.Caret, state.Anchor));
        }
        state.Redo.Clear();

        text = string.Concat(text.AsSpan(0, start), insert, text.AsSpan(end));
        state.Known = text;
        state.Selection.Select(start + insert.Length, start + insert.Length);
        state.LastEdit = kind;
    }

    private static void Restore(TextFieldState state, ref string text, List<TextSnapshot> from, List<TextSnapshot> to)
    {
        if (from.Count == 0) return;
        to.Add(new TextSnapshot(text, state.Caret, state.Anchor));
        var snapshot = from[^1];
        from.RemoveAt(from.Count - 1);
        text = snapshot.Text;
        state.Known = text;
        state.Select(snapshot.Anchor, snapshot.Caret);
    }

    private static int StepLeft(string text, int caret, bool byWord) =>
        byWord ? TextBoundaries.PrevWord(text, caret, splitPunctuation: true) : TextBoundaries.Prev(text, caret);

    private static int StepRight(string text, int caret, bool byWord) =>
        byWord ? TextBoundaries.NextWord(text, caret, splitPunctuation: true) : TextBoundaries.Next(text, caret);
}
