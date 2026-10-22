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

    /// <summary>Edits <paramref name="text"/> in place. Returns true when Enter is pressed while
    /// the field has focus. Supports mouse selection (drag, shift-click, double-click for a word,
    /// triple-click for everything), word-wise movement and deletion, the clipboard, and undo. The
    /// field is a Tab stop. <paramref name="leadingIcon"/> is a glyph drawn before the text in
    /// <paramref name="iconFamily"/>; <paramref name="hint"/> is muted text at the end — a shortcut —
    /// left out while the field is too narrow to keep room for typing.</summary>
    public static bool TextField(this Ui ui, string id, ref string text, string? placeholder = null, Sizing? width = null, string? leadingIcon = null,
        string? iconFamily = null, string? hint = null) =>
        Field(ui, id, ref text, placeholder, width, leadingIcon, iconFamily, hint, inline: false);

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
        return Field(ui, id, ref text, null, width, null, null, null, inline: true) ? InlineEditResult.Committed : InlineEditResult.Editing;
    }

    private static bool Field(Ui ui, string id, ref string text, string? placeholder, Sizing? width, string? leadingIcon, string? iconFamily,
        string? hint, bool inline)
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
            state.Selection.Press(text, ui.CaretAt(text, ui.Input.Pointer.X - textLeft + state.Shift), i.ClickCount,
                extend: focused && (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
            state.LastEdit = EditKind.None;
            ui.Focus(i.Id);
            focused = true;
        }
        else if (i.Held && focused && hasRect)
        {
            state.Selection.Drag(text, ui.CaretAt(text, ui.Input.Pointer.X - textLeft + state.Shift));
            state.LastEdit = EditKind.None;
            var pointerX = ui.Input.Pointer.X;
            if (pointerX < textLeft || pointerX > textLeft + innerWidth)
                ui.RequestFrame();
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
            if (caretX - state.Shift > innerWidth) state.Shift = caretX - innerWidth;
            if (caretX < state.Shift) state.Shift = caretX;
            state.Shift = MathF.Max(0f, MathF.Min(state.Shift, ui.MeasureText(shown) - innerWidth));

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
            Background = theme.Field,
            BorderColor = focused ? theme.FocusRing : i.Hovered ? theme.BorderStrong : theme.Border,
            BorderWidth = theme.BorderWidth,
            Radius = inline ? theme.RadiusSmall : theme.CornerRadius,
        }))
        {
            if (leadingIcon != null)
                ui.Text(leadingIcon, new Box { Width = theme.IconSize }, color: theme.TextMuted, size: theme.IconSize, align: Align.Center, family: iconFamily);

            using (ui.Row(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Clip = true, ScrollX = shift }))
            {
                if (focused && state.HasSelection)
                {
                    var from = MathF.Min(caretX, anchorX) - shift;
                    var to = MathF.Max(caretX, anchorX) - shift;
                    ui.Paint((from, to, lineHeight, theme.Selection), static (p, s) =>
                    {
                        var r = p.Rect;
                        var left = MathF.Max(r.X + s.from, r.X);
                        var right = MathF.Min(r.X + s.to, r.Right);
                        if (right > left)
                            p.FillRect(new UiRect(left, r.Y + (r.Height - s.lineHeight) / 2, right - left, s.lineHeight), s.Selection);
                    });
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
        ui.Paint((left, right, lineHeight, thickness, ui.Theme.Text), static (p, s) =>
        {
            var r = p.Rect;
            var x0 = MathF.Max(r.X + s.left, r.X);
            var x1 = MathF.Min(r.X + s.right, r.Right);
            if (x1 > x0)
                p.FillRect(new UiRect(x0, r.Y + (r.Height + s.lineHeight) / 2 - s.thickness, x1 - x0, s.thickness), s.Text);
        });
    }

    private static bool CaretVisible(Ui ui, TextFieldState state)
    {
        var phase = (ui.RealTime - state.BlinkFrom) % (CaretBlinkSeconds * 2);
        ui.RequestFrameIn(CaretBlinkSeconds - phase % CaretBlinkSeconds);
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
            switch (stroke.Key)
            {
                case KeyboardKey.LeftArrow:
                    Move(state, !extend && !byWord && state.HasSelection ? state.SelectionStart
                        : byWord ? PreviousWord(text, caret)
                        : caret - StepBack(text, caret), extend);
                    break;
                case KeyboardKey.RightArrow:
                    Move(state, !extend && !byWord && state.HasSelection ? state.SelectionEnd
                        : byWord ? NextWord(text, caret)
                        : caret + StepForward(text, caret), extend);
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
                    Replace(state, ref text, byWord ? PreviousWord(text, caret) : caret - StepBack(text, caret), caret, "", EditKind.Deleting);
                    break;
                case KeyboardKey.Delete when caret < text.Length:
                    Replace(state, ref text, caret, byWord ? NextWord(text, caret) : caret + StepForward(text, caret), "", EditKind.Deleting);
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

    private static int StepBack(string text, int caret) =>
        caret == 0 ? 0 : caret >= 2 && char.IsSurrogatePair(text[caret - 2], text[caret - 1]) ? 2 : 1;

    private static int StepForward(string text, int caret) =>
        caret >= text.Length ? 0 : caret + 1 < text.Length && char.IsSurrogatePair(text[caret], text[caret + 1]) ? 2 : 1;

    private enum CharClass
    {
        Space,
        Word,
        Symbol,
    }

    private static CharClass Classify(char c) =>
        char.IsWhiteSpace(c) ? CharClass.Space
        : char.IsLetterOrDigit(c) || c == '_' || char.IsSurrogate(c) ? CharClass.Word
        : CharClass.Symbol;

    internal static (int Start, int End) WordAt(string text, int index)
    {
        if (text.Length == 0) return (0, 0);
        var probe = index < text.Length ? index : index - 1;
        var kind = Classify(text[probe]);
        var start = probe;
        while (start > 0 && Classify(text[start - 1]) == kind) start--;
        var end = probe + 1;
        while (end < text.Length && Classify(text[end]) == kind) end++;
        return (start, end);
    }

    internal static int PreviousWord(string text, int index)
    {
        while (index > 0 && Classify(text[index - 1]) == CharClass.Space) index--;
        if (index == 0) return 0;
        var kind = Classify(text[index - 1]);
        while (index > 0 && Classify(text[index - 1]) == kind) index--;
        return index;
    }

    internal static int NextWord(string text, int index)
    {
        if (index < text.Length && Classify(text[index]) != CharClass.Space)
        {
            var kind = Classify(text[index]);
            while (index < text.Length && Classify(text[index]) == kind) index++;
        }
        while (index < text.Length && Classify(text[index]) == CharClass.Space) index++;
        return index;
    }
}
