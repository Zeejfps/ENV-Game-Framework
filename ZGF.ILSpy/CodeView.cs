using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using TextOverflow = ZGF.Gui.Immediate.TextOverflow;

namespace ZGF.ILSpy;

/// <summary>Where a code view stood: the line at its top, its sideways scroll and its selection —
/// what going back to a place restores.</summary>
public readonly record struct CodeViewPosition(int TopLine, float ScrollX, TextPosition Anchor, TextPosition Caret);

/// <summary>Everything a <see cref="CodeViewWidget.CodeView"/> keeps between frames: scroll,
/// selection, folds, find. One per open tab, owned by the app.</summary>
public sealed class CodeViewState
{
    internal const int NoLine = -1;

    internal readonly CodeViewFrame Frame = new();
    internal readonly CodeFindIndex Find = new();
    internal readonly CodeFolds Folds = new();

    internal DecompiledDocument? Document;
    internal int MaxColumns;
    internal TextPosition Anchor;
    internal TextPosition Caret;
    internal int Clicks;
    internal TextPosition UnitStart;
    internal TextPosition UnitEnd;
    internal TextPosition PressedAt;
    internal bool PressMoved;
    internal bool PressedForNewTab;
    internal bool PressedOnFold;
    internal int PressedSpanLine = NoLine;
    internal int PressedSpan = NoLine;
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal float LaidOutScrollY;
    internal float LaidOutScrollX;
    internal CodeReference? Marked;
    internal CodeReference? MenuReference;
    internal int MenuLine = NoLine;
    internal int PendingReveal = NoLine;
    internal int PendingShowLine = NoLine;
    internal int PendingColumnStart = NoLine;
    internal int PendingColumnEnd;
    internal CodeViewPosition? PendingRestore;
    internal int FlashLine = NoLine;
    internal double FlashFrom;
    internal bool FindOpen;
    internal bool FocusFind;
    internal bool FoldChord;
    internal string Query = "";
    internal int CurrentMatch = NoLine;
    internal int KeyScrollRows;
    internal int KeyScrollPages;

    /// <summary>Scrolls <paramref name="line"/> to about a third from the top of the view and
    /// flashes it, expanding the folds that hide it, once the view has been laid out.</summary>
    public void RevealLine(int line)
    {
        PendingReveal = line;
        PendingRestore = null;
    }

    /// <summary>Where the view stands now.</summary>
    public CodeViewPosition Position
    {
        get
        {
            var top = RowHeight > 0f && Folds.RowCount > 0 ? Folds.LineAt(Math.Clamp((int)(ScrollY / RowHeight), 0, Folds.RowCount - 1)) : 0;
            return new CodeViewPosition(top, ScrollX, Anchor, Caret);
        }
    }

    /// <summary>Returns the view to <paramref name="position"/> once the next document is shown —
    /// instead of revealing a line.</summary>
    public void Restore(CodeViewPosition position)
    {
        PendingRestore = position;
        PendingReveal = NoLine;
    }

    public bool HasSelection => Anchor != Caret;

    /// <summary>The selection, start before end.</summary>
    public (TextPosition Start, TextPosition End) Selection => (TextPosition.Min(Anchor, Caret), TextPosition.Max(Anchor, Caret));

    /// <summary>The selected text of the document last shown, lines joined by '\n', folded lines included.</summary>
    public string SelectedText => Document == null ? "" : CodeText.Between(Document, Selection.Start, Selection.End);

    /// <summary>Whether fold <paramref name="fold"/> of the shown document's
    /// <see cref="DecompiledDocument.Folds"/> is collapsed.</summary>
    public bool IsCollapsed(int fold) => Folds.IsCollapsed(fold);

    public void SetCollapsed(int fold, bool collapsed)
    {
        var top = TopAnchor();
        Folds.SetCollapsed(fold, collapsed);
        KeepTop(top);
    }

    /// <summary>Collapses or expands the innermost fold around <paramref name="line"/>.</summary>
    public void ToggleFold(int line)
    {
        var fold = Folds.Innermost(line);
        if (fold >= 0)
            SetCollapsed(fold, !Folds.IsCollapsed(fold));
    }

    public void CollapseAll() => SetAll(true);

    public void ExpandAll() => SetAll(false);

    internal void Show(DecompiledDocument document)
    {
        if (ReferenceEquals(document, Document)) return;
        Document = document;
        Folds.Reset(document);
        var max = 0;
        foreach (var line in document.Lines)
            max = Math.Max(max, line.Text.Length);
        foreach (var fold in document.Folds)
            max = Math.Max(max, document.Lines[fold.StartLine].Text.Length + 1 + fold.CollapsedText.Length);
        MaxColumns = max;
        Anchor = Caret = UnitStart = UnitEnd = PressedAt = default;
        Clicks = 0;
        PressedSpanLine = PressedSpan = NoLine;
        PressedOnFold = false;
        ScrollX = ScrollY = LaidOutScrollX = LaidOutScrollY = 0f;
        Marked = MenuReference = null;
        MenuLine = NoLine;
        PendingShowLine = PendingColumnStart = NoLine;
        FlashLine = NoLine;
        CurrentMatch = NoLine;
        KeyScrollRows = KeyScrollPages = 0;
        FoldChord = false;
    }

    internal void RevealFolds(int line)
    {
        var top = TopAnchor();
        if (Folds.Reveal(line))
            KeepTop(top);
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

    private void SetAll(bool collapsed)
    {
        var top = TopAnchor();
        Folds.SetAll(collapsed);
        KeepTop(top);
    }

    private (int Line, float Within) TopAnchor()
    {
        if (RowHeight <= 0f || Folds.RowCount == 0) return (0, 0f);
        var row = Math.Clamp((int)(ScrollY / RowHeight), 0, Folds.RowCount - 1);
        return (Folds.LineAt(row), ScrollY - row * RowHeight);
    }

    private void KeepTop((int Line, float Within) top)
    {
        if (RowHeight > 0f)
            ScrollY = Folds.RowOf(top.Line) * RowHeight + top.Within;
    }
}

public readonly record struct CodeViewResult(CodeReference? Navigate, bool NavigateInNewTab);

public static class CodeViewWidget
{
    private const float FlashSeconds = 1.2f;
    private const float AutoScrollSpeed = 12f;
    private const int KeyScrollColumns = 4;
    private const float GutterPaddingLeft = 8f;
    private const float GutterPaddingRight = 8f;
    private const float CodePaddingLeft = 12f;

    /// <summary>
    /// A read-only, virtualized, syntax-colored view of <paramref name="document"/> with line
    /// numbers, folding, selection and copy, find (Ctrl+F) and reference navigation: a click on a
    /// reference asks to navigate to it, a Ctrl+click or middle click asks for a new tab.
    /// </summary>
    public static CodeViewResult CodeView(this Ui ui, string id, DecompiledDocument document, CodeViewState state)
    {
        using var scope = ui.Id(id);
        state.Show(document);
        var theme = ui.Theme;
        var palette = ui.Get<CodeViewPalette>() ?? CodeViewPalette.Dark;
        var cell = ui.MeasureCell(theme.MonoFontSize, family: MonoFonts.Regular);
        var advance = cell.Advance;
        var rowHeight = MathF.Max(theme.CodeLineHeight, MathF.Ceiling(cell.Height));
        state.RowHeight = rowHeight;
        var folds = state.Folds;
        var digits = Math.Max(3, CodeText.Digits(document.Lines.Length));
        var foldLeft = GutterPaddingLeft + digits * advance + GutterPaddingRight;
        var foldWidth = document.Folds.Length > 0 ? theme.IconSize : 0f;
        var gutterWidth = foldLeft + foldWidth;
        var textLeft = gutterWidth + CodePaddingLeft;
        var contentWidth = textLeft + state.MaxColumns * advance + CodePaddingLeft;

        var focusId = ui.GetId("focus");
        var findId = ui.GetId("find");
        var codeId = ui.GetId("code");
        var focused = ui.HasFocus(focusId);
        var findFocused = state.FindOpen && ui.HasFocus(findId);
        var result = default(CodeViewResult);

        if (focused || findFocused)
            HandleKeys(ui, state, document, focusId, focused, advance);

        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Background = theme.EditorBackground }))
        {
            if (state.FindOpen)
                FindBar(ui, state, document, focusId, findId);
            ui.TabStop(focusId);

            var i = ui.Interact(codeId);
            var hasRect = ui.TryGetRect(codeId, out var rect);
            ui.TryGetContentRect(codeId, out var content);

            TextPosition PositionAt(float x, float y)
            {
                var row = (int)MathF.Floor((y - rect.Y + state.LaidOutScrollY) / rowHeight);
                var column = (int)MathF.Round((x - rect.X - textLeft + state.LaidOutScrollX) / advance);
                if (row < 0) return CodeText.Clamp(document, new TextPosition(0, 0));
                if (row >= folds.RowCount) return CodeText.End(document);
                var line = folds.LineAt(row);
                var fold = folds.CollapsedAt(line);
                if (fold >= 0 && column > document.Lines[line].Text.Length)
                {
                    var end = document.Folds[fold].EndLine;
                    return new TextPosition(end, document.Lines[end].Text.Length);
                }
                return CodeText.Clamp(document, new TextPosition(line, column));
            }

            var pointer = ui.Input.Pointer;
            var hoverLine = CodeViewState.NoLine;
            var hoverSpan = CodeViewState.NoLine;
            var hoverFold = CodeViewState.NoLine;
            var hoverChip = CodeViewState.NoLine;
            if (hasRect && folds.RowCount > 0 && (i.Hovered || i.Held) && pointer.X < content.Right && pointer.Y < content.Bottom)
            {
                var row = (int)MathF.Floor((pointer.Y - rect.Y + state.LaidOutScrollY) / rowHeight);
                if (row >= 0 && row < folds.RowCount)
                {
                    var line = folds.LineAt(row);
                    if (pointer.X >= rect.X + gutterWidth)
                    {
                        var column = (int)MathF.Floor((pointer.X - rect.X - textLeft + state.LaidOutScrollX) / advance);
                        if (column >= 0)
                        {
                            hoverLine = line;
                            hoverSpan = CodeText.SpanAt(document.Lines[line].Spans, column);
                            var collapsed = folds.CollapsedAt(line);
                            var chip = document.Lines[line].Text.Length + 1;
                            if (collapsed >= 0 && column >= chip && column < chip + document.Folds[collapsed].CollapsedText.Length)
                                hoverChip = collapsed;
                        }
                    }
                    else if (foldWidth > 0f && pointer.X >= rect.X + foldLeft)
                    {
                        hoverFold = document.FoldStartingAt(line);
                    }
                }
            }
            var hoverReference = hoverSpan >= 0 ? document.Lines[hoverLine].Spans[hoverSpan].Reference : null;
            var hoverNavigable = hoverReference is { IsNavigable: true } && !document.Lines[hoverLine].Spans[hoverSpan].IsDefinition;
            var hoverToggle = hoverFold >= 0 || hoverChip >= 0;

            if (i.Hovered || i.Held)
                ui.SetCursor((hoverNavigable || hoverToggle) && !state.PressMoved ? MouseCursor.Hand : MouseCursor.Text);

            var scrollDeltaY = 0f;
            if (i.Pressed && hasRect)
            {
                ui.Focus(focusId, takesText: false);
                state.PressMoved = false;
                state.PressedOnFold = hoverToggle;
                state.PressedSpanLine = state.PressedSpan = CodeViewState.NoLine;
                if (hoverFold >= 0)
                {
                    state.SetCollapsed(hoverFold, !folds.IsCollapsed(hoverFold));
                }
                else if (hoverChip >= 0)
                {
                    state.SetCollapsed(hoverChip, false);
                }
                else
                {
                    var at = PositionAt(pointer.X, pointer.Y);
                    state.PressedAt = at;
                    state.PressedForNewTab = (ui.Input.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
                    (state.PressedSpanLine, state.PressedSpan) = (hoverLine, hoverSpan);
                    Press(state, document, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
                }
            }
            else if (i.Held && hasRect && !state.PressedOnFold)
            {
                var at = PositionAt(pointer.X, pointer.Y);
                if (!state.PressMoved && at != state.PressedAt)
                    state.PressMoved = true;
                if (state.PressMoved)
                    Drag(state, document, at);

                var dy = pointer.Y < content.Y ? pointer.Y - content.Y : pointer.Y > content.Bottom ? pointer.Y - content.Bottom : 0f;
                var left = rect.X + gutterWidth;
                var dx = pointer.X < left ? pointer.X - left : pointer.X > content.Right ? pointer.X - content.Right : 0f;
                if (dy != 0f || dx != 0f)
                {
                    scrollDeltaY = AutoScroll(dy, rowHeight, ui.DeltaTime);
                    state.ScrollX = MathF.Max(0f, state.ScrollX + AutoScroll(dx, advance, ui.DeltaTime));
                    ui.RequestFrame();
                }
            }

            if (i.Clicked && !state.PressMoved && !state.PressedOnFold && state.Clicks == 1 && (ui.Input.Modifiers & KeyModifiers.Shift) == 0)
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
                state.MenuLine = hasRect ? PositionAt(pointer.X, pointer.Y).Line : CodeViewState.NoLine;
                ui.OpenContextMenu("menu");
            }

            Scroll(ui, state, document, rowHeight, advance, textLeft, content, scrollDeltaY);
            FillFrame(ui, state, document, theme, palette, cell, rowHeight, foldLeft, foldWidth, gutterWidth, textLeft, digits, focused || findFocused,
                hoverLine, hoverSpan, hoverReference, hoverNavigable, hoverFold, hoverChip);

            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f }))
            {
                using (var list = ui.VirtualScroll(codeId, folds.RowCount, rowHeight, ref state.ScrollX, ref state.ScrollY,
                           new Box { Gap = 0f, Background = theme.EditorBackground }))
                {
                    for (var row = list.First; row < list.End; row++)
                    {
                        using (ui.Box(new Box { Width = Sizing.Grow(min: contentWidth), Height = rowHeight }))
                            ui.Paint((state.Frame, row), CodeViewFrame.PaintRow);
                    }
                }
                if (focused && ui.FocusVisible)
                {
                    using (ui.Float(0f, 0f, box: new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }, z: 1))
                        ui.FocusRing(0f);
                }
            }
            state.LaidOutScrollX = state.ScrollX;
            state.LaidOutScrollY = state.ScrollY;
        }

        if (ContextMenu(ui, state, document) is { Navigate: not null } picked)
            result = picked;
        return result;
    }

    private static float AutoScroll(float distance, float unit, float deltaTime) =>
        distance == 0f ? 0f : (distance * AutoScrollSpeed + MathF.Sign(distance) * unit * AutoScrollSpeed) * deltaTime;

    private static void FillFrame(Ui ui, CodeViewState state, DecompiledDocument document, UiTheme theme, CodeViewPalette palette, CellMetrics cell,
        float rowHeight, float foldLeft, float foldWidth, float gutterWidth, float textLeft, int digits, bool active, int hoverLine, int hoverSpan,
        CodeReference? hoverReference, bool hoverNavigable, int hoverFold, int hoverChip)
    {
        var frame = state.Frame;
        var (start, end) = state.Selection;
        frame.Document = document;
        frame.Folds = state.Folds;
        frame.Palette = palette;
        frame.Background = theme.EditorBackground;
        frame.GutterHover = theme.Text;
        frame.Selection = active ? theme.Selection : theme.SelectionInactive;
        frame.ErrorText = theme.Danger;
        frame.FontSize = theme.MonoFontSize;
        frame.SeparatorWidth = theme.SeparatorWidth;
        frame.OutlineWidth = theme.BorderWidth;
        frame.ChipRadius = theme.RadiusSmall;
        frame.GutterPaddingLeft = GutterPaddingLeft;
        frame.Advance = cell.Advance;
        frame.CellHeight = cell.Height;
        frame.RowHeight = rowHeight;
        frame.FoldLeft = foldLeft;
        frame.FoldWidth = foldWidth;
        frame.GutterWidth = gutterWidth;
        frame.TextLeft = textLeft;
        frame.Digits = digits;
        frame.SelectionStart = start;
        frame.SelectionEnd = end;
        frame.Highlight = hoverReference ?? state.Marked;
        (frame.UnderlineLine, frame.UnderlineSpan) = hoverNavigable && !state.PressMoved ? (hoverLine, hoverSpan) : (CodeViewState.NoLine, CodeViewState.NoLine);
        frame.HoverFold = hoverFold;
        frame.HoverChip = hoverChip;

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
                frame.Flash = UiColor.Mix(theme.EditorBackground, palette.Flash, 1f - t);
                ui.RequestFrame();
            }
        }
    }

    private static void Scroll(Ui ui, CodeViewState state, DecompiledDocument document, float rowHeight, float advance, float textLeft,
        UiRect content, float deltaY)
    {
        var folds = state.Folds;
        var viewport = content.Height;
        if (state.PendingRestore is { } restore)
        {
            if (viewport <= 0f)
            {
                ui.RequestFrame();
            }
            else
            {
                state.PendingRestore = null;
                var top = Math.Clamp(restore.TopLine, 0, Math.Max(0, document.Lines.Length - 1));
                state.RevealFolds(top);
                state.ScrollY = folds.RowOf(top) * rowHeight;
                state.ScrollX = restore.ScrollX;
                state.Select(CodeText.Clamp(document, restore.Anchor), CodeText.Clamp(document, restore.Caret));
            }
        }

        if (state.KeyScrollPages != 0 && viewport > 0f)
        {
            deltaY += state.KeyScrollPages * MathF.Max(1f, MathF.Floor(viewport / rowHeight) - 1f) * rowHeight;
            state.KeyScrollPages = 0;
        }
        deltaY += state.KeyScrollRows * rowHeight;
        state.KeyScrollRows = 0;
        if (deltaY != 0f)
            state.ScrollY = Math.Clamp(state.ScrollY + deltaY, 0f, MathF.Max(0f, folds.RowCount * rowHeight - viewport));

        if (state.PendingReveal != CodeViewState.NoLine || state.PendingShowLine != CodeViewState.NoLine)
        {
            if (viewport <= 0f)
            {
                ui.RequestFrame();
            }
            else if (state.PendingReveal != CodeViewState.NoLine)
            {
                var line = Math.Clamp(state.PendingReveal, 0, Math.Max(0, document.Lines.Length - 1));
                state.RevealFolds(line);
                state.ScrollY = MathF.Max(0f, folds.RowOf(line) * rowHeight - viewport / 3f);
                state.FlashLine = line;
                state.FlashFrom = ui.RealTime;
                state.PendingReveal = CodeViewState.NoLine;
                state.PendingShowLine = CodeViewState.NoLine;
                ui.RequestFrame();
            }
            else
            {
                state.RevealFolds(state.PendingShowLine);
                var top = folds.RowOf(state.PendingShowLine) * rowHeight;
                if (top < state.ScrollY || top + rowHeight > state.ScrollY + viewport)
                    state.ScrollY = MathF.Max(0f, top - viewport / 3f);
                state.PendingShowLine = CodeViewState.NoLine;
            }
        }

        if (state.PendingColumnStart != CodeViewState.NoLine && content.Width > 0f)
        {
            var visible = content.Width - textLeft - advance;
            var from = state.PendingColumnStart * advance;
            var to = state.PendingColumnEnd * advance;
            if (from < state.ScrollX)
                state.ScrollX = MathF.Max(0f, from - 4f * advance);
            else if (to > state.ScrollX + visible)
                state.ScrollX = to - visible + 4f * advance;
            state.PendingColumnStart = CodeViewState.NoLine;
        }
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
                (state.UnitStart, state.UnitEnd) = RowAt(state, document, at.Line);
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
            _ => RowAt(state, document, at.Line),
        };
        if (state.Clicks == 1)
            state.Select(state.Anchor, at);
        else if (start < state.UnitStart)
            state.Select(state.UnitEnd, start);
        else
            state.Select(state.UnitStart, TextPosition.Max(end, state.UnitEnd));
    }

    private static (TextPosition Start, TextPosition End) RowAt(CodeViewState state, DecompiledDocument document, int line)
    {
        var first = state.Folds.VisibleLine(line);
        var next = state.Folds.NextLine(first);
        return next < document.Lines.Length
            ? (new TextPosition(first, 0), new TextPosition(next, 0))
            : (new TextPosition(first, 0), CodeText.End(document));
    }

    private static void HandleKeys(Ui ui, CodeViewState state, DecompiledDocument document, int focusId, bool focused, float advance)
    {
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            var stroke = keys[k];
            var shortcut = (stroke.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
            var shift = (stroke.Modifiers & KeyModifiers.Shift) != 0;
            var chord = state.FoldChord;
            state.FoldChord = false;
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
                case KeyboardKey.M when focused && chord:
                    state.ToggleFold(state.Caret.Line);
                    break;
                case KeyboardKey.L when focused && chord:
                    if (state.Folds.AnyCollapsed) state.ExpandAll();
                    else state.CollapseAll();
                    break;
                case KeyboardKey.M when focused && shortcut && !shift:
                    state.FoldChord = true;
                    break;
                case KeyboardKey.LeftBracket when focused && shortcut && shift:
                    FoldAtCaret(state, collapse: true);
                    break;
                case KeyboardKey.RightBracket when focused && shortcut && shift:
                    FoldAtCaret(state, collapse: false);
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
                    state.ScrollX = MathF.Max(0f, state.ScrollX - KeyScrollColumns * advance);
                    break;
                case KeyboardKey.RightArrow when focused:
                    state.ScrollX += KeyScrollColumns * advance;
                    break;
            }
        }
    }

    private static void FoldAtCaret(CodeViewState state, bool collapse)
    {
        var fold = state.Folds.Innermost(state.Caret.Line, collapsed: !collapse);
        if (fold >= 0)
            state.SetCollapsed(fold, collapse);
    }

    private static void FindBar(Ui ui, CodeViewState state, DecompiledDocument document, int focusId, int findId)
    {
        var theme = ui.Theme;
        using (ui.Row(new Box
        {
            Height = theme.ToolbarHeight,
            Padding = new Spacing(theme.ShellPaddingX, theme.ToolbarPaddingY),
            Gap = theme.GapTight,
            AlignY = Align.Center,
            Background = theme.Surface,
        }))
        {
            if (state.FocusFind)
            {
                ui.Focus(findId);
                state.FocusFind = false;
            }

            var submitted = ui.TextField("find", ref state.Query, "Find", Sizing.Grow(AppLayout.FindMinWidth, AppLayout.FindWidth));
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
            ui.Text(label, new Box { Width = Sizing.Grow() }, color: count == 0 && state.Query.Length > 0 ? theme.Warning : theme.TextMuted,
                size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
            if (ui.IconButton("previous", LucideIcons.ChevronUp, LucideIcons.Family, "Previous match (Shift+F3)", count > 0))
                Step(state, document, -1);
            if (ui.IconButton("next", LucideIcons.ChevronDown, LucideIcons.Family, "Next match (F3)", count > 0))
                Step(state, document, 1);
            if (ui.IconButton("close", LucideIcons.X, LucideIcons.Family, "Close (Esc)"))
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
        state.RevealFolds(match.Line);
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
        if (document.Folds.Length > 0)
        {
            ui.MenuSeparator();
            if (ui.MenuItem("Toggle fold", "Ctrl+M, M", enabled: state.MenuLine >= 0 && state.Folds.Innermost(state.MenuLine) >= 0))
                state.ToggleFold(state.MenuLine);
            if (ui.MenuItem("Collapse all"))
                state.CollapseAll();
            if (ui.MenuItem("Expand all"))
                state.ExpandAll();
        }
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
