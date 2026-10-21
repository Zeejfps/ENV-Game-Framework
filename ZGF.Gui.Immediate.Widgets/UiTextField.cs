using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Immediate.Widgets;

public static partial class UiWidgets
{
    private const float FieldPaddingX = 8f;
    private const float CaretWidth = 1.5f;
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
        public int Caret;
        public int Anchor;
        public float Shift;
        public string Known = "";
        public EditKind LastEdit;
        public readonly List<TextSnapshot> Undo = new();
        public readonly List<TextSnapshot> Redo = new();
        public int Clicks;
        public int UnitStart;
        public int UnitEnd;
        public double BlinkFrom;

        public bool HasSelection => Caret != Anchor;
        public int SelectionStart => Math.Min(Caret, Anchor);
        public int SelectionEnd => Math.Max(Caret, Anchor);

        public void Select(int anchor, int caret)
        {
            Anchor = anchor;
            Caret = caret;
            LastEdit = EditKind.None;
        }
    }

    /// <summary>Edits <paramref name="text"/> in place. Returns true when Enter is pressed while
    /// the field has focus. Supports mouse selection (drag, shift-click, double-click for a word,
    /// triple-click for everything), word-wise movement and deletion, the clipboard, and undo.</summary>
    public static bool TextField(this Ui ui, string id, ref string text, string? placeholder = null, Sizing? width = null)
    {
        var theme = WidgetTheme.For(ui);
        var style = ui.Style;
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

        var focused = ui.HasFocus(i.Id);
        var (caretBefore, anchorBefore, textBefore) = (state.Caret, state.Anchor, text);
        var hasRect = ui.TryGetRect(i.Id, out var rect);
        var innerWidth = hasRect ? rect.Width - FieldPaddingX * 2 : float.PositiveInfinity;

        if (i.Pressed)
        {
            if (!focused) state.Shift = 0f;
            Press(ui, state, text, CaretAt(ui, text, ui.Input.Pointer.X - rect.X - FieldPaddingX + state.Shift), i.ClickCount, extend: focused);
            ui.Focus(i.Id);
            focused = true;
        }
        else if (i.Held && focused && hasRect)
        {
            Drag(state, text, CaretAt(ui, text, ui.Input.Pointer.X - rect.X - FieldPaddingX + state.Shift));
            var pointerX = ui.Input.Pointer.X;
            if (pointerX < rect.X + FieldPaddingX || pointerX > rect.Right - FieldPaddingX)
                ui.RequestFrame();
        }

        var submitted = false;
        if (focused)
        {
            submitted = Edit(ui, state, ref text);
            focused = ui.HasFocus(i.Id);
        }

        var caretX = 0f;
        var anchorX = 0f;
        if (focused)
        {
            caretX = ui.MeasureText(text, state.Caret);
            anchorX = state.HasSelection ? ui.MeasureText(text, state.Anchor) : caretX;
            if (caretX - state.Shift > innerWidth) state.Shift = caretX - innerWidth;
            if (caretX < state.Shift) state.Shift = caretX;
            state.Shift = MathF.Max(0f, MathF.Min(state.Shift, ui.MeasureText(text) - innerWidth));

            if (state.Caret != caretBefore || state.Anchor != anchorBefore || !ReferenceEquals(text, textBefore) || i.Pressed)
                state.BlinkFrom = ui.RealTime;
        }
        var shift = focused ? state.Shift : 0f;

        using (ui.Row(i.Id, new Box
        {
            Width = width ?? Sizing.Grow(MinFieldWidth),
            Height = theme.ControlHeight,
            Padding = new Spacing(FieldPaddingX, 0f),
            Clip = true,
            ScrollX = shift,
            Background = style.Field,
            BorderColor = focused ? style.Accent : i.Hovered ? style.TextMuted : style.Border,
            BorderWidth = 1f,
            Radius = theme.CornerRadius,
        }))
        {
            var lineHeight = ui.LineHeight();
            if (focused && state.HasSelection)
            {
                var from = MathF.Min(caretX, anchorX) - shift;
                var to = MathF.Max(caretX, anchorX) - shift;
                ui.Paint((from, to, lineHeight, style.Selection), static (p, s) =>
                {
                    var r = p.Rect;
                    var left = MathF.Max(r.X + FieldPaddingX + s.from, r.X + 1f);
                    var right = MathF.Min(r.X + FieldPaddingX + s.to, r.Right - 1f);
                    if (right > left)
                        p.FillRect(new UiRect(left, r.Y + (r.Height - s.lineHeight) / 2, right - left, s.lineHeight), s.Selection);
                });
            }

            if (text.Length > 0)
                ui.Text(text);
            else if (placeholder != null)
                ui.Text(placeholder, color: style.TextMuted);

            if (focused && !state.HasSelection && CaretVisible(ui, state))
            {
                using (ui.Float(0f, 0.5f, FieldPaddingX + caretX - shift, -lineHeight / 2,
                    new Box { Width = CaretWidth, Height = lineHeight, Background = style.Text }, z: 1)) { }
            }
        }
        return submitted;
    }

    private static bool CaretVisible(Ui ui, TextFieldState state)
    {
        var phase = (ui.RealTime - state.BlinkFrom) % (CaretBlinkSeconds * 2);
        ui.RequestFrameIn(CaretBlinkSeconds - phase % CaretBlinkSeconds);
        return phase < CaretBlinkSeconds;
    }

    private static void Press(Ui ui, TextFieldState state, string text, int at, int clickCount, bool extend)
    {
        state.Clicks = (clickCount - 1) % 3 + 1;

        switch (state.Clicks)
        {
            case 1 when extend && (ui.Input.Modifiers & KeyModifiers.Shift) != 0:
                state.Select(state.Anchor, at);
                state.UnitStart = state.UnitEnd = state.Anchor;
                break;
            case 1:
                state.Select(at, at);
                state.UnitStart = state.UnitEnd = at;
                break;
            case 2:
                (state.UnitStart, state.UnitEnd) = WordAt(text, at);
                state.Select(state.UnitStart, state.UnitEnd);
                break;
            default:
                (state.UnitStart, state.UnitEnd) = (0, text.Length);
                state.Select(0, text.Length);
                break;
        }
    }

    private static void Drag(TextFieldState state, string text, int at)
    {
        switch (state.Clicks)
        {
            case 1:
                state.Select(state.Anchor, at);
                break;
            case 2:
                var (start, end) = WordAt(text, at);
                if (start < state.UnitStart)
                    state.Select(state.UnitEnd, start);
                else
                    state.Select(state.UnitStart, Math.Max(end, state.UnitEnd));
                break;
        }
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
                    ui.ClipboardText = Selected(state, text);
                    break;
                case KeyboardKey.X when shortcut && state.HasSelection:
                    ui.ClipboardText = Selected(state, text);
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

    private static string Selected(TextFieldState state, string text) =>
        text.Substring(state.SelectionStart, state.SelectionEnd - state.SelectionStart);

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
        state.Caret = state.Anchor = start + insert.Length;
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
