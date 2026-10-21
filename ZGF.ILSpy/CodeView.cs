using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy;

/// <summary>Everything a <see cref="CodeViewWidget.CodeView"/> keeps between frames: scroll,
/// selection, find. One per open tab, owned by the app.</summary>
public sealed class CodeViewState
{
    internal const int NoLine = -1;

    internal readonly CodeViewFrame Frame = new();
    internal readonly CodeFindIndex Find = new();

    internal DecompiledDocument? Document;
    internal int Generation;
    internal int MaxColumns;
    internal TextPosition Anchor;
    internal TextPosition Caret;
    internal int Clicks;
    internal TextPosition UnitStart;
    internal TextPosition UnitEnd;
    internal TextPosition PressedAt;
    internal bool PressMoved;
    internal bool PressedForNewTab;
    internal int PressedSpanLine = NoLine;
    internal int PressedSpan = NoLine;
    internal float ScrollX;
    internal float LaidOutScrollY;
    internal float LaidOutScrollX;
    internal float ThumbAnchor;
    internal float ThumbStartScroll;
    internal CodeReference? Marked;
    internal CodeReference? MenuReference;
    internal int PendingReveal = NoLine;
    internal int PendingShowLine = NoLine;
    internal int PendingColumnStart = NoLine;
    internal int PendingColumnEnd;
    internal int FlashLine = NoLine;
    internal double FlashFrom;
    internal bool FindOpen;
    internal bool FocusFind;
    internal string Query = "";
    internal int CurrentMatch = NoLine;
    internal int KeyScrollRows;
    internal int KeyScrollPages;

    /// <summary>Scrolls <paramref name="line"/> to about a third from the top of the view and
    /// flashes it, once the view has been laid out.</summary>
    public void RevealLine(int line) => PendingReveal = line;

    public bool HasSelection => Anchor != Caret;

    /// <summary>The selection, start before end.</summary>
    public (TextPosition Start, TextPosition End) Selection => (TextPosition.Min(Anchor, Caret), TextPosition.Max(Anchor, Caret));

    /// <summary>The selected text of the document last shown, lines joined by '\n'.</summary>
    public string SelectedText => Document == null ? "" : CodeText.Between(Document, Selection.Start, Selection.End);

    internal void Show(DecompiledDocument document)
    {
        if (ReferenceEquals(document, Document)) return;
        Document = document;
        Generation++;
        var max = 0;
        foreach (var line in document.Lines)
            max = Math.Max(max, line.Text.Length);
        MaxColumns = max;
        Anchor = Caret = UnitStart = UnitEnd = PressedAt = default;
        Clicks = 0;
        PressedSpanLine = PressedSpan = NoLine;
        ScrollX = LaidOutScrollX = LaidOutScrollY = 0f;
        Marked = MenuReference = null;
        PendingShowLine = PendingColumnStart = NoLine;
        FlashLine = NoLine;
        CurrentMatch = NoLine;
        KeyScrollRows = KeyScrollPages = 0;
    }

    internal void ShowColumns(int start, int end)
    {
        PendingColumnStart = start;
        PendingColumnEnd = end;
    }

    internal void Select(TextPosition anchor, TextPosition caret)
    {
        Anchor = anchor;
        Caret = caret;
    }
}

public readonly record struct CodeViewResult(CodeReference? Navigate, bool NavigateInNewTab);

public static class CodeViewWidget
{
    private const float FlashSeconds = 1.2f;
    private const float AutoScrollSpeed = 12f;
    private const float ThumbMinLength = 24f;
    private const int KeyScrollColumns = 4;

    /// <summary>
    /// A read-only, virtualized, syntax-colored view of <paramref name="document"/> with line
    /// numbers, selection and copy, find (Ctrl+F) and reference navigation: a click on a reference
    /// asks to navigate to it, a Ctrl+click or middle click asks for a new tab.
    /// </summary>
    public static CodeViewResult CodeView(this Ui ui, string id, DecompiledDocument document, CodeViewState state)
    {
        using var scope = ui.Id(id);
        state.Show(document);
        var theme = ui.Theme;
        var cell = ui.MeasureCell(family: MonoFonts.Regular);
        var advance = cell.Advance;
        var rowHeight = MathF.Ceiling(cell.Height);
        var digits = Math.Max(3, CodeText.Digits(document.Lines.Length));
        var gutterWidth = (digits + 2) * advance;
        var textLeft = gutterWidth + advance;
        var frame = state.Frame;

        var focusId = ui.GetId("focus");
        var findId = ui.GetId("find");
        var codeId = HashCode.Combine(ui.GetId("code"), state.Generation);
        var focused = ui.HasFocus(focusId);
        var findFocused = state.FindOpen && ui.HasFocus(findId);
        var result = default(CodeViewResult);

        if (focused || findFocused)
            HandleKeys(ui, state, document, focusId, focused, advance);

        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Background = theme.Field }))
        {
            if (state.FindOpen)
                FindBar(ui, state, document, focusId, findId);

            var i = ui.Interact(codeId);
            var hasRect = ui.TryGetRect(codeId, out var rect);
            var viewWidth = frame.ViewWidth > 0f ? frame.ViewWidth : hasRect ? rect.Width : 0f;
            var contentWidth = textLeft + state.MaxColumns * advance + advance;
            var maxScrollX = MathF.Max(0f, contentWidth - viewWidth);

            TextPosition PositionAt(float x, float y)
            {
                var line = (int)MathF.Floor((y - rect.Y + state.LaidOutScrollY) / rowHeight);
                var column = (int)MathF.Round((x - rect.X - textLeft + state.LaidOutScrollX) / advance);
                return CodeText.Clamp(document, new TextPosition(line, column));
            }

            var pointer = ui.Input.Pointer;
            var hoverLine = CodeViewState.NoLine;
            var hoverSpan = CodeViewState.NoLine;
            if (hasRect && document.Lines.Length > 0 && (i.Hovered || i.Held) && pointer.X >= rect.X + gutterWidth)
            {
                var row = (int)MathF.Floor((pointer.Y - rect.Y + state.LaidOutScrollY) / rowHeight);
                var column = (int)MathF.Floor((pointer.X - rect.X - textLeft + state.LaidOutScrollX) / advance);
                if (row >= 0 && row < document.Lines.Length && column >= 0)
                {
                    hoverLine = row;
                    hoverSpan = CodeText.SpanAt(document.Lines[row].Spans, column);
                }
            }
            var hoverReference = hoverSpan >= 0 ? document.Lines[hoverLine].Spans[hoverSpan].Reference : null;
            var hoverNavigable = hoverReference is { IsNavigable: true } && !document.Lines[hoverLine].Spans[hoverSpan].IsDefinition;

            if (i.Hovered || i.Held)
                ui.SetCursor(hoverNavigable && !state.PressMoved ? MouseCursor.Hand : MouseCursor.Text);

            var scrollDeltaY = 0f;
            if (i.Pressed && hasRect)
            {
                ui.Focus(focusId, takesText: false);
                var at = PositionAt(pointer.X, pointer.Y);
                state.PressedAt = at;
                state.PressMoved = false;
                state.PressedForNewTab = (ui.Input.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
                (state.PressedSpanLine, state.PressedSpan) = (hoverLine, hoverSpan);
                Press(state, document, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
            }
            else if (i.Held && hasRect)
            {
                var at = PositionAt(pointer.X, pointer.Y);
                if (!state.PressMoved && at != state.PressedAt)
                    state.PressMoved = true;
                if (state.PressMoved)
                    Drag(state, document, at);

                var dy = pointer.Y < rect.Y ? pointer.Y - rect.Y : pointer.Y > rect.Bottom ? pointer.Y - rect.Bottom : 0f;
                var left = rect.X + gutterWidth;
                var right = rect.X + viewWidth;
                var dx = pointer.X < left ? pointer.X - left : pointer.X > right ? pointer.X - right : 0f;
                if (dy != 0f || dx != 0f)
                {
                    scrollDeltaY = AutoScroll(dy, rowHeight, ui.DeltaTime);
                    state.ScrollX += AutoScroll(dx, advance, ui.DeltaTime);
                    ui.RequestFrame();
                }
            }

            if (i.Clicked && !state.PressMoved && state.Clicks == 1 && (ui.Input.Modifiers & KeyModifiers.Shift) == 0)
            {
                var reference = state.PressedSpan >= 0 ? document.Lines[state.PressedSpanLine].Spans[state.PressedSpan] : default;
                if (reference.Reference is { IsNavigable: true } target && !reference.IsDefinition)
                    result = new CodeViewResult(target, state.PressedForNewTab);
                else
                    state.Marked = reference.Reference;
            }
            if (i.ClickedWith(PointerButton.Middle) && hoverNavigable)
                result = new CodeViewResult(hoverReference, true);
            if (i.ClickedWith(PointerButton.Right))
            {
                ui.Focus(focusId, takesText: false);
                state.MenuReference = hoverNavigable ? hoverReference : null;
                ui.OpenContextMenu("menu");
            }

            if (i.Hovered && ui.Input.ScrollX != 0f)
                state.ScrollX -= ui.Input.ScrollX * theme.ScrollWheelStep;
            if (state.PendingColumnStart != CodeViewState.NoLine && viewWidth > 0f)
            {
                var visible = viewWidth - textLeft - advance;
                var from = state.PendingColumnStart * advance;
                var to = state.PendingColumnEnd * advance;
                if (from < state.ScrollX)
                    state.ScrollX = from - 4f * advance;
                else if (to > state.ScrollX + visible)
                    state.ScrollX = to - visible + 4f * advance;
                state.PendingColumnStart = CodeViewState.NoLine;
            }
            state.ScrollX = Math.Clamp(state.ScrollX, 0f, maxScrollX);

            var thumbId = ui.GetId("hthumb");
            var thumb = ui.Interact(thumbId);
            var thumbLength = viewWidth <= 0f ? 0f : MathF.Min(viewWidth, MathF.Max(ThumbMinLength, viewWidth * viewWidth / contentWidth));
            var travel = viewWidth - thumbLength;
            if (thumb.Pressed)
            {
                state.ThumbAnchor = pointer.X;
                state.ThumbStartScroll = state.ScrollX;
            }
            else if (thumb.Held && travel > 0f)
            {
                state.ScrollX = Math.Clamp(state.ThumbStartScroll + (pointer.X - state.ThumbAnchor) * maxScrollX / travel, 0f, maxScrollX);
            }

            FillFrame(ui, state, document, theme, cell, rowHeight, gutterWidth, textLeft, digits, hoverLine, hoverSpan, hoverReference, hoverNavigable);

            using (var list = ui.VirtualScroll(codeId, document.Lines.Length, rowHeight, new Box { Gap = 0f, Background = theme.Field }))
            {
                state.LaidOutScrollY = list.Offset;
                state.LaidOutScrollX = state.ScrollX;
                for (var line = list.First; line < list.End; line++)
                {
                    using (ui.Box(new Box { Width = Sizing.Grow(), Height = rowHeight }))
                        ui.Paint((frame, line), CodeViewFrame.PaintRow);
                }

                ApplyScroll(ui, state, list, rowHeight, scrollDeltaY);

                if (maxScrollX > 0f && thumbLength > 0f)
                {
                    var bar = theme.ScrollBarWidth;
                    var color = thumb.Held ? theme.ScrollThumbHeld : thumb.Hovered ? theme.ScrollThumbHovered : theme.ScrollThumb;
                    using (ui.Float(0f, 1f, travel * state.ScrollX / maxScrollX, -bar - 1f,
                        new Box { Width = thumbLength, Height = bar, Background = color, Radius = bar / 2 }, z: 1, id: thumbId)) { }
                }
            }
        }

        if (ContextMenu(ui, state, document) is { Navigate: not null } picked)
            result = picked;
        return result;
    }

    private static float AutoScroll(float distance, float unit, float deltaTime) =>
        distance == 0f ? 0f : (distance * AutoScrollSpeed + MathF.Sign(distance) * unit * AutoScrollSpeed) * deltaTime;

    private static void FillFrame(Ui ui, CodeViewState state, DecompiledDocument document, UiTheme theme, CellMetrics cell, float rowHeight,
        float gutterWidth, float textLeft, int digits, int hoverLine, int hoverSpan, CodeReference? hoverReference, bool hoverNavigable)
    {
        var frame = state.Frame;
        var (start, end) = state.Selection;
        frame.Document = document;
        frame.Palette = CodeViewPalette.For(theme);
        frame.Background = theme.Field;
        frame.GutterText = theme.TextMuted;
        frame.GutterBorder = UiColor.Mix(theme.Field, theme.Border, 0.6f);
        frame.Selection = theme.Selection;
        frame.Occurrence = UiColor.Mix(theme.Field, theme.Accent, 0.2f);
        frame.Advance = cell.Advance;
        frame.CellHeight = cell.Height;
        frame.RowHeight = rowHeight;
        frame.GutterWidth = gutterWidth;
        frame.TextLeft = textLeft;
        frame.ScrollX = state.ScrollX;
        frame.Digits = digits;
        frame.SelectionStart = start;
        frame.SelectionEnd = end;
        frame.Highlight = hoverReference ?? state.Marked;
        (frame.UnderlineLine, frame.UnderlineSpan) = hoverNavigable && !state.PressMoved ? (hoverLine, hoverSpan) : (CodeViewState.NoLine, CodeViewState.NoLine);

        var findActive = state.FindOpen && state.Query.Length > 0;
        frame.Matches = state.Find.Matches;
        frame.MatchLength = findActive ? state.Find.Length : 0;
        frame.CurrentMatch = state.CurrentMatch;

        frame.FlashLine = CodeViewState.NoLine;
        if (state.FlashLine != CodeViewState.NoLine)
        {
            var t = (float)((ui.RealTime - state.FlashFrom) / FlashSeconds);
            if (t >= 1f)
            {
                state.FlashLine = CodeViewState.NoLine;
            }
            else
            {
                frame.FlashLine = state.FlashLine;
                frame.Flash = UiColor.Mix(theme.Field, theme.Accent, 0.45f * (1f - t));
                ui.RequestFrame();
            }
        }
    }

    private static void ApplyScroll(Ui ui, CodeViewState state, Ui.VirtualList list, float rowHeight, float deltaY)
    {
        if (state.KeyScrollPages != 0 && list.Viewport > 0f)
        {
            deltaY += state.KeyScrollPages * MathF.Max(1f, MathF.Floor(list.Viewport / rowHeight) - 1f) * rowHeight;
            state.KeyScrollPages = 0;
        }
        deltaY += state.KeyScrollRows * rowHeight;
        state.KeyScrollRows = 0;
        if (deltaY != 0f)
            list.ScrollTo(list.Offset + deltaY);

        if (state.PendingReveal == CodeViewState.NoLine && state.PendingShowLine == CodeViewState.NoLine) return;
        if (list.Viewport <= 0f)
        {
            ui.RequestFrame();
            return;
        }

        if (state.PendingReveal != CodeViewState.NoLine)
        {
            var line = Math.Clamp(state.PendingReveal, 0, Math.Max(0, list.Count - 1));
            list.ScrollTo(line * rowHeight - list.Viewport / 3f);
            state.FlashLine = line;
            state.FlashFrom = ui.RealTime;
            state.PendingReveal = CodeViewState.NoLine;
            ui.RequestFrame();
        }
        else
        {
            var top = state.PendingShowLine * rowHeight;
            if (top < list.Offset || top + rowHeight > list.Offset + list.Viewport)
                list.ScrollTo(top - list.Viewport / 3f);
        }
        state.PendingShowLine = CodeViewState.NoLine;
    }

    private static void Press(CodeViewState state, DecompiledDocument document, TextPosition at, int clickCount, bool extend)
    {
        state.Clicks = (Math.Max(1, clickCount) - 1) % 3 + 1;
        switch (state.Clicks)
        {
            case 1 when extend:
                state.Select(state.Anchor, at);
                state.UnitStart = state.UnitEnd = state.Anchor;
                break;
            case 1:
                state.Select(at, at);
                state.UnitStart = state.UnitEnd = at;
                break;
            case 2:
                (state.UnitStart, state.UnitEnd) = CodeText.WordAt(document, at);
                state.Select(state.UnitStart, state.UnitEnd);
                break;
            default:
                (state.UnitStart, state.UnitEnd) = CodeText.LineAt(document, at.Line);
                state.Select(state.UnitStart, state.UnitEnd);
                break;
        }
    }

    private static void Drag(CodeViewState state, DecompiledDocument document, TextPosition at)
    {
        var (start, end) = state.Clicks switch
        {
            1 => (at, at),
            2 => CodeText.WordAt(document, at),
            _ => CodeText.LineAt(document, at.Line),
        };
        if (state.Clicks == 1)
            state.Select(state.Anchor, at);
        else if (start < state.UnitStart)
            state.Select(state.UnitEnd, start);
        else
            state.Select(state.UnitStart, TextPosition.Max(end, state.UnitEnd));
    }

    private static void HandleKeys(Ui ui, CodeViewState state, DecompiledDocument document, int focusId, bool focused, float advance)
    {
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            var stroke = keys[k];
            var shortcut = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            var shift = (stroke.Modifiers & KeyModifiers.Shift) != 0;
            switch (stroke.Key)
            {
                case KeyboardKey.F when shortcut && !shift && (stroke.Modifiers & KeyModifiers.Alt) == 0:
                    if (!state.FindOpen && state.HasSelection && state.Anchor.Line == state.Caret.Line)
                        state.Query = state.SelectedText;
                    state.FindOpen = true;
                    state.FocusFind = true;
                    break;
                case KeyboardKey.F3 when state.Query.Length > 0:
                    state.FindOpen = true;
                    Step(state, document, shift ? -1 : 1);
                    break;
                case KeyboardKey.Escape when state.FindOpen:
                    state.FindOpen = false;
                    ui.Focus(focusId, takesText: false);
                    break;
                case KeyboardKey.A when shortcut && focused:
                    state.Select(default, CodeText.End(document));
                    break;
                case KeyboardKey.C when shortcut && focused && state.HasSelection:
                    ui.ClipboardText = state.SelectedText;
                    break;
                case KeyboardKey.UpArrow when focused:
                    state.KeyScrollRows--;
                    break;
                case KeyboardKey.DownArrow when focused:
                    state.KeyScrollRows++;
                    break;
                case KeyboardKey.PageUp when focused:
                    state.KeyScrollPages--;
                    break;
                case KeyboardKey.PageDown when focused:
                    state.KeyScrollPages++;
                    break;
                case KeyboardKey.Home when focused:
                    state.KeyScrollRows -= document.Lines.Length;
                    break;
                case KeyboardKey.End when focused:
                    state.KeyScrollRows += document.Lines.Length;
                    break;
                case KeyboardKey.LeftArrow when focused:
                    state.ScrollX -= KeyScrollColumns * advance;
                    break;
                case KeyboardKey.RightArrow when focused:
                    state.ScrollX += KeyScrollColumns * advance;
                    break;
            }
        }
    }

    private static void FindBar(Ui ui, CodeViewState state, DecompiledDocument document, int focusId, int findId)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box { Padding = new Spacing(6f, 4f), Gap = 6f, Background = theme.Surface }))
        {
            if (state.FocusFind)
            {
                ui.Focus(findId);
                state.FocusFind = false;
            }

            var submitted = ui.TextField("find", ref state.Query, "Find", Sizing.Fixed(260f));
            var queryChanged = !string.Equals(state.Query, state.Find.Query, StringComparison.Ordinal);
            if (state.Find.Update(document, state.Query))
            {
                state.CurrentMatch = queryChanged ? state.Find.FirstFrom(state.Selection.Start) : CodeViewState.NoLine;
                ShowMatch(state);
            }
            if (submitted)
                Step(state, document, (ui.Input.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);

            var count = state.Find.Matches.Count;
            var label = state.Query.Length == 0 ? ""
                : count == 0 ? "No results"
                : ui.Memo("count", (state.CurrentMatch, count), static a => a.CurrentMatch >= 0 ? $"{a.CurrentMatch + 1} of {a.count}" : $"{a.count} matches");
            ui.Text(label, color: count == 0 && state.Query.Length > 0 ? CodeViewPalette.For(theme).Error : theme.TextMuted);
            ui.Leaf(new Box { Width = Sizing.Grow(), Height = 0f });
            if (ui.Button("Previous", enabled: count > 0))
                Step(state, document, -1);
            if (ui.Button("Next", enabled: count > 0))
                Step(state, document, 1);
            if (ui.Button("Close"))
            {
                state.FindOpen = false;
                ui.Focus(focusId, takesText: false);
            }
        }
    }

    private static void Step(CodeViewState state, DecompiledDocument document, int direction)
    {
        state.Find.Update(document, state.Query);
        var count = state.Find.Matches.Count;
        if (count == 0)
        {
            state.CurrentMatch = CodeViewState.NoLine;
            return;
        }
        state.CurrentMatch = state.CurrentMatch < 0
            ? direction > 0 ? state.Find.FirstFrom(state.Caret) : (state.Find.FirstOnOrAfter(state.Caret) - 1 + count) % count
            : (state.CurrentMatch + direction + count) % count;
        ShowMatch(state);
    }

    private static void ShowMatch(CodeViewState state)
    {
        if (state.CurrentMatch < 0) return;
        var match = state.Find.Matches[state.CurrentMatch];
        state.Select(match, match with { Column = match.Column + state.Find.Length });
        state.PendingShowLine = match.Line;
        state.ShowColumns(match.Column, match.Column + state.Find.Length);
    }

    private static CodeViewResult ContextMenu(Ui ui, CodeViewState state, DecompiledDocument document)
    {
        var result = default(CodeViewResult);
        using var menu = ui.ContextMenu("menu");
        if (!menu.IsOpen) return result;

        if (ui.MenuItem("Copy", "Ctrl+C", enabled: state.HasSelection))
            ui.ClipboardText = state.SelectedText;
        if (ui.MenuItem("Select all", "Ctrl+A"))
            state.Select(default, CodeText.End(document));
        if (state.MenuReference is { } reference)
        {
            ui.MenuSeparator();
            if (ui.MenuItem("Go to definition"))
                result = new CodeViewResult(reference, false);
            if (ui.MenuItem("Open in new tab"))
                result = new CodeViewResult(reference, true);
        }
        return result;
    }
}
