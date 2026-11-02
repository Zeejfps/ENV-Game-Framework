using ZGF.Desktop;
using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using TextOverflow = ZGF.Gui.Immediate.TextOverflow;

namespace ZGF.ILSpy;

/// <summary>The selection in a code view, whose triple press selects the row there: a line, or a
/// collapsed fold with the lines it hides.</summary>
public sealed class CodeSelection : TextSelection<DecompiledDocument, TextPosition>
{
    private readonly CodeFolds _folds;

    internal CodeSelection(CodeFolds folds) => _folds = folds;

    protected override (TextPosition Start, TextPosition End) WordAt(DecompiledDocument document, TextPosition at) => CodeText.WordAt(document, at);

    protected override (TextPosition Start, TextPosition End) BlockAt(DecompiledDocument document, TextPosition at)
    {
        var first = new TextPosition(_folds.VisibleLine(at.Line), 0);
        var next = _folds.NextLine(first.Line);
        if (next >= document.Lines.Length)
            return (first, CodeText.End(document));
        return (first, new TextPosition(next, 0));
    }
}

/// <summary>The press held on a code view, or last held: on a bookmark mark, a fold's chevron or a collapsed
/// fold, which a drag or click leaves alone; or on the text.</summary>
internal abstract class CodePress
{
    private CodePress()
    {
    }

    public sealed class OnControl : CodePress
    {
    }

    /// <summary>A press on the text at <see cref="At"/>, with Ctrl held when <see cref="ForNewTab"/>, on the
    /// span <see cref="Span"/> of a line or none.</summary>
    public sealed class OnText : CodePress
    {
        public OnText(TextPosition at, bool forNewTab, CodeSpanAt? span)
        {
            At = at;
            ForNewTab = forNewTab;
            Span = span;
        }

        public TextPosition At { get; }

        public bool ForNewTab { get; }

        public CodeSpanAt? Span { get; }

        /// <summary>Whether the press has moved off where it started, dragging a selection.</summary>
        public bool Moved;
    }
}

/// <summary>The span <see cref="Span"/> of the line <see cref="Line"/> of a document.</summary>
internal readonly record struct CodeSpanAt(int Line, int Span);

/// <summary>Where a code view is to scroll once it is laid out: to a line revealed and flashed, back to where
/// it stood, or to a find match.</summary>
internal abstract record PendingScroll
{
    private PendingScroll()
    {
    }

    /// <summary>Scroll <see cref="Line"/> to about a third from the top, expanding the folds that hide it, and flash it.</summary>
    public sealed record Reveal(int Line) : PendingScroll;

    public sealed record Restore(CodeViewPosition Position) : PendingScroll;

    /// <summary>Bring the columns from <see cref="ColumnStart"/> to <see cref="ColumnEnd"/> of <see cref="Line"/> into view.</summary>
    public sealed record ShowMatch(int Line, int ColumnStart, int ColumnEnd) : PendingScroll;
}

/// <summary>What the pointer is over in a code view this frame: nothing; the text of a line, on one of its spans
/// or none and on a collapsed fold's chip or none; a fold's chevron; or the bookmark margin of a line. A struct
/// with a case for each, as it is made every frame.</summary>
internal readonly struct CodeHover
{
    private enum Place : byte
    {
        Nothing,
        Text,
        Chevron,
        Mark,
    }

    private readonly Place _place;
    private readonly int _line;
    private readonly int? _span;
    private readonly int? _fold;

    private CodeHover(Place place, int line, int? span, int? fold)
    {
        _place = place;
        _line = line;
        _span = span;
        _fold = fold;
    }

    public static CodeHover Nothing => default;

    /// <summary>Over the text of <paramref name="line"/>, on its span <paramref name="span"/> or none, and on
    /// the chip of the collapsed fold <paramref name="chip"/> or none.</summary>
    public static CodeHover OnText(int line, int? span, int? chip) => new(Place.Text, line, span, chip);

    public static CodeHover OnChevron(int fold) => new(Place.Chevron, 0, null, fold);

    public static CodeHover OnMark(int line) => new(Place.Mark, line, null, null);

    /// <summary>The span under the pointer, or null.</summary>
    public CodeSpanAt? Span => _place == Place.Text && _span != null ? new CodeSpanAt(_line, _span.Value) : null;

    /// <summary>The collapsed fold whose chip is under the pointer, or null.</summary>
    public int? Chip => _place == Place.Text ? _fold : null;

    /// <summary>The fold whose chevron is under the pointer, or null.</summary>
    public int? Chevron => _place == Place.Chevron ? _fold : null;

    /// <summary>The line whose bookmark margin is under the pointer, or null.</summary>
    public int? Mark => _place == Place.Mark ? _line : null;

    /// <summary>Whether a press here works a control, a chip, chevron or mark, rather than the text.</summary>
    public bool IsOnControl => Chip != null || Chevron != null || Mark != null;
}

/// <summary>The line <see cref="Line"/> flashing since the time <see cref="From"/>, after it was revealed.</summary>
internal readonly record struct CodeFlash(int Line, double From);

/// <summary>Everything a <see cref="CodeViewWidget.CodeView"/> keeps between frames: scroll,
/// selection, folds, find. One per open tab, owned by the app.</summary>
public sealed class CodeViewState
{
    internal readonly CodeViewFrame Frame = new();
    internal readonly CodeFindIndex Find = new();
    internal readonly CodeFolds Folds = new();
    internal readonly MatchingBrackets Brackets = new();

    internal DecompiledDocument? Document;
    internal int MaxColumns;
    internal CodePress? Press;
    internal float ScrollX;
    internal float ScrollY;
    internal float RowHeight;
    internal float LaidOutScrollY;
    internal float LaidOutScrollX;
    internal CodeReference? Marked;
    internal CodeMenuTarget Menu = CodeMenuTarget.None;
    internal readonly KeptTooltip Tooltip = new();
    internal PendingScroll? Pending;
    internal CodeFlash? Flash;
    internal bool FindOpen;
    internal bool FocusFind;
    internal string Query = "";
    /// <summary>The index of the find match selected, or null; kept while find is closed, for F3 to go on from.</summary>
    internal int? CurrentMatch;
    internal int KeyScrollRows;
    internal int KeyScrollPages;

    public CodeViewState() => Selection = new CodeSelection(Folds);

    public CodeSelection Selection { get; }

    /// <summary>Scrolls <paramref name="line"/> to about a third from the top of the view and
    /// flashes it, expanding the folds that hide it, once the view has been laid out.</summary>
    public void RevealLine(int line) => Pending = new PendingScroll.Reveal(line);

    /// <summary>Where the view stands now.</summary>
    public CodeViewPosition Position
    {
        get
        {
            var top = RowHeight > 0f && Folds.RowCount > 0 ? Folds.LineAt(Folds.Clamp(CodeRow.At(ScrollY, RowHeight))) : 0;
            return new CodeViewPosition(top, ScrollX, Selection.Anchor, Selection.Caret);
        }
    }

    /// <summary>Returns the view to <paramref name="position"/> once the next document is shown —
    /// instead of revealing a line.</summary>
    public void Restore(CodeViewPosition position) => Pending = new PendingScroll.Restore(position);

    /// <summary>Whether a press on the text has moved off where it started, dragging a selection.</summary>
    internal bool PressMoved => Press is CodePress.OnText text && text.Moved;

    public bool HasSelection => Selection.HasSelection;

    /// <summary>The selected text of the document last shown, lines joined by '\n', folded lines included.</summary>
    public string SelectedText => Document == null ? "" : CodeText.Between(Document, Selection.Start, Selection.End);

    /// <summary>The selected text as HTML in <paramref name="style"/>, a div per line.</summary>
    internal string SelectedHtml(CodeHtmlStyle style) => Document == null ? "" : CodeHtml.Between(Document, Selection.Start, Selection.End, style);

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
        if (fold != null)
            SetCollapsed(fold.Value, !Folds.IsCollapsed(fold.Value));
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
        Selection.Select(default, default);
        Press = null;
        ScrollX = ScrollY = LaidOutScrollX = LaidOutScrollY = 0f;
        Marked = null;
        Menu = CodeMenuTarget.None;
        Tooltip.Forget();
        if (Pending is PendingScroll.ShowMatch)
            Pending = null;
        Flash = null;
        CurrentMatch = null;
        KeyScrollRows = KeyScrollPages = 0;
    }

    internal void RevealFolds(int line)
    {
        var top = TopAnchor();
        if (Folds.Reveal(line))
            KeepTop(top);
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
        var row = Folds.Clamp(CodeRow.At(ScrollY, RowHeight));
        return (Folds.LineAt(row), ScrollY - row.Top(RowHeight));
    }

    private void KeepTop((int Line, float Within) top)
    {
        if (RowHeight > 0f)
            ScrollY = Folds.RowOf(top.Line).Top(RowHeight) + top.Within;
    }
}

/// <summary>How the user asked a code view to change the size of its code.</summary>
public enum CodeZoom
{
    None,
    In,
    Out,
    Reset,
}

/// <summary>What the user asked a code view to do, besides changing the size of its code.</summary>
public abstract record CodeViewIntent
{
    private CodeViewIntent()
    {
    }

    /// <summary>Go to <see cref="Target"/>, in a new tab when <see cref="InNewTab"/>.</summary>
    public sealed record Navigate(CodeReference.Navigable Target, bool InNewTab) : CodeViewIntent;

    public sealed record ToggleBookmark(int Line) : CodeViewIntent;

    public sealed record Analyze(CodeReference.Entity Entity) : CodeViewIntent;

    public sealed record ShowInMetadata(CodeReference.Navigable Target) : CodeViewIntent;

    public sealed record ShowInApiReference(CodeReference.Entity Entity) : CodeViewIntent;

    public sealed record ShowInDiagram(CodeReference.Entity Type) : CodeViewIntent;

    public sealed record ShowInNewDiagram(CodeReference.Entity Type) : CodeViewIntent;
}

/// <summary>What a frame of a code view asked for: what the user asked it to do, the last thing when more
/// than one, or null; and how to change the size of its code.</summary>
public readonly record struct CodeViewResult(CodeViewIntent? Intent, CodeZoom Zoom);

public static class CodeViewWidget
{
    private static readonly DisplayOptions Plain = new(HighlightMatchingBrackets: false);

    private const float FlashSeconds = 1.2f;
    private const float AutoScrollSpeed = 12f;
    private const int KeyScrollColumns = 4;
    private const float GutterPaddingLeft = 8f;
    private const float GutterPaddingRight = 8f;
    private const float CodePaddingLeft = 12f;

    /// <summary>
    /// A read-only, virtualized, syntax-colored view of <paramref name="document"/> with line
    /// numbers, folding, selection and copy, find (Ctrl+F) and reference navigation: a click on a
    /// reference asks to navigate to it, a Ctrl+click or middle click asks for a new tab. The lines in
    /// <paramref name="bookmarks"/> are marked in the gutter, dimmed when not enabled; a click in the
    /// margin before the line numbers, or Toggle bookmark in the context menu, asks to toggle one.
    /// Without <paramref name="bookmarks"/>, no line can be. Without a <paramref name="gutter"/>, there
    /// are no line numbers, fold chevrons or bookmark marks, only the text. With
    /// <paramref name="tooltips"/>, resting the pointer on a type or member shows its declaration and
    /// documentation. With <paramref name="display"/>, the code is drawn at its size with the aids it
    /// turns on, and Ctrl with the wheel, =, - or 0 asks to change the size; without, at the theme's
    /// size with line numbers only.
    /// </summary>
    public static CodeViewResult CodeView(this Ui<UiState> ui, string id, DecompiledDocument document, CodeViewState state,
        IReadOnlyDictionary<int, bool>? bookmarks = null, bool gutter = true, CodeTooltips? tooltips = null, DisplayOptions? display = null)
    {
        using var scope = ui.Id(id);
        state.Show(document);
        var theme = ui.Theme;
        var palette = ui.State.CodeViewPalette;
        var shown = display ?? Plain;
        var fontSize = shown.FontSizeIn(theme);
        var cell = ui.MeasureCell(fontSize, family: MonoFonts.Code);
        var advance = cell.Advance;
        var rowHeight = RowHeightFor(theme, fontSize, cell);
        state.RowHeight = rowHeight;
        var folds = state.Folds;
        var digits = LineNumberDigits(document, shown);
        var markWidth = gutter ? theme.IconSize + GutterPaddingLeft : 0f;
        var foldLeft = gutter ? markWidth + digits * advance + GutterPaddingRight : 0f;
        var foldWidth = gutter && document.Folds.Length > 0 ? theme.IconSize : 0f;
        var gutterWidth = foldLeft + foldWidth;
        var textLeft = gutterWidth + CodePaddingLeft;
        var contentWidth = textLeft + state.MaxColumns * advance + CodePaddingLeft;

        var focusId = ui.GetId("focus");
        var findId = ui.GetId("find");
        var codeId = ui.GetId("code");
        var focused = ui.HasFocus(focusId);
        var findFocused = state.FindOpen && ui.HasFocus(findId);
        CodeViewIntent? intent = null;
        var zoom = CodeZoom.None;

        if (focused || findFocused)
        {
            HandleKeys(ui, state, document, focusId, focused, advance);
            if (focused && ui.IsPressed(Shortcuts.Analyze))
            {
                var analyzed = AnalyzableAt(document, state.Selection.Caret);
                if (analyzed != null)
                    intent = new CodeViewIntent.Analyze(analyzed);
            }
        }

        using (ui.Column(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f, Background = theme.EditorBackground }))
        {
            if (state.FindOpen)
            {
                FindBar(ui, state, document, focusId, findId);
                ui.Separator();
            }
            ui.TabStop(focusId);

            var i = ui.Interact(codeId);
            if (display != null)
                zoom = ZoomAsked(ui, focused, i.Hovered);
            var hasRect = ui.TryGetRect(codeId, out var rect);
            ui.TryGetContentRect(codeId, out var content);

            TextPosition PositionAt(float x, float y)
            {
                var row = CodeRow.At(y - rect.Y + state.LaidOutScrollY, rowHeight);
                var column = (int)MathF.Round((x - rect.X - textLeft + state.LaidOutScrollX) / advance);
                if (row.Index < 0) return CodeText.Clamp(document, new TextPosition(0, 0));
                if (row.Index >= folds.RowCount) return CodeText.End(document);
                var line = folds.LineAt(row);
                var fold = folds.CollapsedAt(line);
                if (fold != null && column > document.Lines[line].Text.Length)
                {
                    var end = document.Folds[fold.Value].EndLine;
                    return new TextPosition(end, document.Lines[end].Text.Length);
                }
                return CodeText.Clamp(document, new TextPosition(line, column));
            }

            var pointer = ui.Input.Pointer;
            var hover = CodeHover.Nothing;
            if (hasRect && folds.RowCount > 0 && (i.Hovered || i.Held) && pointer.X < content.Right && pointer.Y < content.Bottom)
            {
                var row = CodeRow.At(pointer.Y - rect.Y + state.LaidOutScrollY, rowHeight);
                if (folds.Contains(row))
                {
                    var line = folds.LineAt(row);
                    if (pointer.X >= rect.X + gutterWidth)
                    {
                        var column = (int)MathF.Floor((pointer.X - rect.X - textLeft + state.LaidOutScrollX) / advance);
                        if (column >= 0)
                        {
                            var span = CodeText.SpanAt(document.Lines[line].Spans, column);
                            var collapsed = folds.CollapsedAt(line);
                            var chipStart = document.Lines[line].Text.Length + 1;
                            int? chip = null;
                            if (collapsed != null && column >= chipStart && column < chipStart + document.Folds[collapsed.Value].CollapsedText.Length)
                                chip = collapsed;
                            hover = CodeHover.OnText(line, span, chip);
                        }
                    }
                    else if (foldWidth > 0f && pointer.X >= rect.X + foldLeft)
                    {
                        var fold = document.FoldStartingAt(line);
                        if (fold != null)
                            hover = CodeHover.OnChevron(fold.Value);
                    }
                    else if (bookmarks != null && pointer.X < rect.X + markWidth)
                    {
                        hover = CodeHover.OnMark(line);
                    }
                }
            }
            var hoverSpan = hover.Span;
            var hovered = hoverSpan != null ? document.Lines[hoverSpan.Value.Line].Spans[hoverSpan.Value.Span] : default;
            var hoverReference = hovered.Reference;
            var hoverTarget = hoverReference is CodeReference.Navigable underPointer && !hovered.IsDefinition ? underPointer : null;
            var hoverNavigable = hoverTarget != null;

            if (i.Hovered || i.Held)
                ui.SetCursor((hoverNavigable || hover.IsOnControl) && !state.PressMoved ? MouseCursor.Hand : MouseCursor.Text);

            var scrollDeltaY = 0f;
            if (i.Pressed && hasRect)
            {
                ui.Focus(focusId, takesText: false);
                state.Press = new CodePress.OnControl();
                var mark = hover.Mark;
                var chevron = hover.Chevron;
                var chip = hover.Chip;
                if (mark != null)
                {
                    intent = new CodeViewIntent.ToggleBookmark(mark.Value);
                }
                else if (chevron != null)
                {
                    state.SetCollapsed(chevron.Value, !folds.IsCollapsed(chevron.Value));
                }
                else if (chip != null)
                {
                    state.SetCollapsed(chip.Value, false);
                }
                else
                {
                    var at = PositionAt(pointer.X, pointer.Y);
                    var forNewTab = (ui.Input.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
                    state.Press = new CodePress.OnText(at, forNewTab, hoverSpan);
                    state.Selection.Press(document, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
                }
            }
            else if (i.Held && hasRect && state.Press is CodePress.OnText held)
            {
                var at = PositionAt(pointer.X, pointer.Y);
                if (!held.Moved && at != held.At)
                    held.Moved = true;
                if (held.Moved)
                    state.Selection.Drag(document, at);

                var dy = pointer.Y < content.Y ? pointer.Y - content.Y : pointer.Y > content.Bottom ? pointer.Y - content.Bottom : 0f;
                var left = rect.X + gutterWidth;
                var dx = pointer.X < left ? pointer.X - left : pointer.X > content.Right ? pointer.X - content.Right : 0f;
                if (dy != 0f || dx != 0f)
                {
                    scrollDeltaY = AutoScroll(dy, rowHeight, ui.DeltaTime);
                    state.ScrollX = MathF.Max(0f, state.ScrollX + AutoScroll(dx, advance, ui.DeltaTime));
                    ui.RequestUpdate();
                }
            }

            if (i.Clicked && state.Press is CodePress.OnText clicked && !clicked.Moved && state.Selection.Clicks == 1 && (ui.Input.Modifiers & KeyModifiers.Shift) == 0)
            {
                var pressedSpan = clicked.Span;
                var reference = pressedSpan != null ? document.Lines[pressedSpan.Value.Line].Spans[pressedSpan.Value.Span] : default;
                if (reference.Reference is CodeReference.Navigable target && !reference.IsDefinition)
                    intent = new CodeViewIntent.Navigate(target, clicked.ForNewTab);
                else
                    state.Marked = reference.Reference;
            }
            if (i.ClickedWith(PointerButton.Middle) && hoverTarget != null)
                intent = new CodeViewIntent.Navigate(hoverTarget, true);
            if (i.ClickedWith(PointerButton.Right))
            {
                ui.Focus(focusId, takesText: false);
                var onDefinition = hoverSpan != null && hovered.IsDefinition;
                int? menuLine = hasRect ? PositionAt(pointer.X, pointer.Y).Line : null;
                state.Menu = new CodeMenuTarget(hoverReference, onDefinition, menuLine);
                ui.OpenContextMenu("menu");
            }

            Scroll(ui, state, document, rowHeight, advance, textLeft, content, scrollDeltaY);
            FillFrame(ui, state, document, theme, palette, cell, rowHeight, markWidth, foldLeft, foldWidth, gutterWidth, textLeft, digits,
                focused || findFocused, bookmarks, hover, hoverReference, hoverNavigable);
            FillDisplayAids(state, document, shown, fontSize);

            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f }))
            {
                using (var list = ui.VirtualScroll(codeId, folds.RowCount, rowHeight, ref state.ScrollX, ref state.ScrollY,
                           new Box { Background = theme.EditorBackground }))
                {
                    for (var row = list.First; row < list.End; row++)
                    {
                        using (ui.Box(new Box { Width = Sizing.Grow(min: contentWidth), Height = rowHeight }))
                            ui.Paint((state.Frame, new CodeRow(row)), CodeViewFrame.PaintRow);
                    }
                }
                if (focused && ui.FocusVisible)
                {
                    using (ui.Float(0f, 0f, box: new Box { Width = Sizing.Grow(), Height = Sizing.Grow() }, z: 1))
                        ui.FocusRing(0f);
                }
            }
            if (tooltips != null && hasRect)
            {
                var under = new HoveredSpan(hoverSpan, rect.X + textLeft, rect.Y);
                var followed = ReferenceTooltip(ui, state, document, tooltips, codeId, under, advance, rowHeight);
                if (followed != null)
                    intent = followed;
            }
            state.LaidOutScrollX = state.ScrollX;
            state.LaidOutScrollY = state.ScrollY;
        }

        var picked = ContextMenu(ui, state, document, bookmarks != null);
        if (picked != null)
            intent = picked;
        return new CodeViewResult(intent, zoom);
    }

    /// <summary>How many digits wide the line numbers are, at least three; none when they are hidden.</summary>
    private static int LineNumberDigits(DecompiledDocument document, DisplayOptions display)
    {
        if (!display.ShowLineNumbers)
            return 0;
        var digits = CodeText.Digits(document.Lines.Length);
        return Math.Max(3, digits);
    }

    /// <summary>The height of a row of code at <paramref name="fontSize"/>: the theme's, grown or shrunk
    /// with the size, and never less than the font's own.</summary>
    private static float RowHeightFor(UiTheme theme, float fontSize, CellMetrics cell)
    {
        var scaled = MathF.Ceiling(theme.CodeLineHeight * fontSize / theme.MonoFontSize);
        var fontHeight = MathF.Ceiling(cell.Height);
        return MathF.Max(scaled, fontHeight);
    }

    /// <summary>The size change asked this frame: Ctrl with =, - or 0 while focused, or Ctrl with the wheel while hovered.</summary>
    private static CodeZoom ZoomAsked(Ui<UiState> ui, bool focused, bool hovered)
    {
        if (focused)
        {
            var pressed = ZoomPressed(ui);
            if (pressed != CodeZoom.None)
                return pressed;
        }
        if (!hovered)
            return CodeZoom.None;
        return ZoomScrolled(ui.Input);
    }

    private static CodeZoom ZoomPressed(Ui<UiState> ui)
    {
        if (Shortcuts.AnyPressed(ui, Shortcuts.ZoomInKeys))
            return CodeZoom.In;
        if (Shortcuts.AnyPressed(ui, Shortcuts.ZoomOutKeys))
            return CodeZoom.Out;
        if (Shortcuts.AnyPressed(ui, Shortcuts.ZoomResetKeys))
            return CodeZoom.Reset;
        return CodeZoom.None;
    }

    private static CodeZoom ZoomScrolled(UiInput input)
    {
        if (!IsShortcut(input.Modifiers))
            return CodeZoom.None;
        if (input.ScrollY > 0f)
            return CodeZoom.In;
        if (input.ScrollY < 0f)
            return CodeZoom.Out;
        return CodeZoom.None;
    }

    // The wheel zooms with Ctrl or Command held, as the framework's own zoom does.
    private static bool IsShortcut(KeyModifiers modifiers) => (modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;

    /// <summary>Fills what <paramref name="display"/> draws around the text: the caret's line and the
    /// brackets matching at the caret.</summary>
    private static void FillDisplayAids(CodeViewState state, DecompiledDocument document, DisplayOptions display, float fontSize)
    {
        var frame = state.Frame;
        var caret = state.Selection.Caret;
        frame.FontSize = fontSize;
        frame.CurrentLine = display.HighlightCurrentLine ? caret.Line : null;
        frame.Brackets = FindBracketsToShow(state, document, display, caret);
    }

    private static BracketPair? FindBracketsToShow(CodeViewState state, DecompiledDocument document, DisplayOptions display, TextPosition caret)
    {
        if (!display.HighlightMatchingBrackets)
            return null;
        return state.Brackets.Find(document, caret);
    }

    /// <summary>The span under the pointer and where the view's text starts on screen.</summary>
    private readonly record struct HoveredSpan(CodeSpanAt? Span, float TextX, float TextY);

    /// <summary>The tooltip of the type or member under the pointer, under its span once it has
    /// been looked up.</summary>
    private static CodeViewIntent? ReferenceTooltip(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, CodeTooltips tooltips,
        int codeId, HoveredSpan hovered, float advance, float rowHeight)
    {
        var tooltip = TooltipOf(state, document, tooltips, hovered);
        var part = 0;
        var span = hovered.Span;
        if (tooltip != null && span != null)
            part = HashCode.Combine(span.Value.Line, span.Value.Span) | 1;
        var anchor = TooltipAnchor(state, document, hovered, advance, rowHeight);
        using var popup = ui.TooltipContent(codeId, part, anchor, out var shownPart, CodeTooltipView.Look);
        state.Tooltip.Keep(tooltip, part, shownPart);
        if (!popup.IsOpen)
            return null;
        var shown = state.Tooltip.For(shownPart);
        if (shown == null)
            return null;
        var click = CodeTooltipView.Render(ui, shown);
        return FollowLink(ui, click);
    }

    /// <summary>A link clicked in the tooltip asks to navigate to it, in a new tab with a middle
    /// click or Ctrl, and closes the tooltip.</summary>
    private static CodeViewIntent? FollowLink(Ui<UiState> ui, RichTextClick click)
    {
        if (click.Link is not CodeReference.Navigable reference)
            return null;
        ui.HideTooltip();
        var withModifier = (ui.Input.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
        var inNewTab = click.Button == PointerButton.Middle || withModifier;
        return new CodeViewIntent.Navigate(reference, inNewTab);
    }

    /// <summary>The tooltip of the span under the pointer when it points at a type or member and
    /// its lookup is done; null otherwise, and while a press drags a selection.</summary>
    private static CodeTooltip? TooltipOf(CodeViewState state, DecompiledDocument document, CodeTooltips tooltips, HoveredSpan hovered)
    {
        var span = hovered.Span;
        if (span == null)
            return null;
        if (state.PressMoved)
            return null;
        if (document.Lines[span.Value.Line].Spans[span.Value.Span].Reference is not CodeReference.Entity entity)
            return null;
        tooltips.TryGet(entity, out var tooltip);
        return tooltip;
    }

    /// <summary>The hovered span, widened to the left by the tooltip's padding so the tooltip's
    /// text starts where the word does.</summary>
    private static UiRect TooltipAnchor(CodeViewState state, DecompiledDocument document, HoveredSpan hovered, float advance, float rowHeight)
    {
        var span = SpanRect(state, document, hovered, advance, rowHeight);
        return span with { X = span.X - CodeTooltipView.PaddingX, Width = span.Width + CodeTooltipView.PaddingX };
    }

    private static UiRect SpanRect(CodeViewState state, DecompiledDocument document, HoveredSpan hovered, float advance, float rowHeight)
    {
        var at = hovered.Span;
        if (at == null)
            return default;
        var span = document.Lines[at.Value.Line].Spans[at.Value.Span];
        var x = hovered.TextX - state.LaidOutScrollX + span.Start * advance;
        var y = hovered.TextY + state.Folds.RowOf(at.Value.Line).Top(rowHeight) - state.LaidOutScrollY;
        return new UiRect(x, y, span.Length * advance, rowHeight);
    }

    private static float AutoScroll(float distance, float unit, float deltaTime) =>
        distance == 0f ? 0f : (distance * AutoScrollSpeed + MathF.Sign(distance) * unit * AutoScrollSpeed) * deltaTime;

    private static void FillFrame(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, UiTheme theme, CodeViewPalette palette, CellMetrics cell,
        float rowHeight, float markWidth, float foldLeft, float foldWidth, float gutterWidth, float textLeft, int digits, bool active,
        IReadOnlyDictionary<int, bool>? bookmarks, CodeHover hover, CodeReference? hoverReference, bool hoverNavigable)
    {
        var frame = state.Frame;
        frame.Document = document;
        frame.Folds = state.Folds;
        frame.Palette = palette;
        frame.Background = theme.EditorBackground;
        frame.GutterHover = theme.Text;
        frame.Selection = active ? theme.TextSelection : theme.SelectionInactive;
        frame.ErrorText = theme.Danger;
        frame.SeparatorWidth = theme.SeparatorWidth;
        frame.OutlineWidth = theme.BorderWidth;
        frame.ChipRadius = theme.RadiusSmall;
        frame.GutterPaddingLeft = markWidth;
        frame.MarkWidth = markWidth;
        frame.IconSize = theme.IconSize;
        frame.BookmarkGlyph = ui.State.Icons.CodeBookmark;
        frame.Bookmarks = bookmarks;
        frame.HoverMark = hover.Mark;
        frame.Advance = cell.Advance;
        frame.CellHeight = cell.Height;
        frame.RowHeight = rowHeight;
        frame.FoldLeft = foldLeft;
        frame.FoldWidth = foldWidth;
        frame.GutterWidth = gutterWidth;
        frame.TextLeft = textLeft;
        frame.Digits = digits;
        frame.SelectionStart = state.Selection.Start;
        frame.SelectionEnd = state.Selection.End;
        frame.Highlight = hoverReference ?? state.Marked;
        frame.Underline = hoverNavigable && !state.PressMoved ? hover.Span : null;
        frame.HoverFold = hover.Chevron;
        frame.HoverChip = hover.Chip;

        var findActive = state.FindOpen && state.Query.Length > 0;
        frame.Matches = state.Find.Matches;
        frame.MatchLength = findActive ? state.Find.Length : 0;
        frame.CurrentMatch = state.CurrentMatch;

        frame.FlashLine = null;
        var flash = state.Flash;
        if (flash != null)
        {
            var t = (float)((ui.RealTime - flash.Value.From) / FlashSeconds);
            if (t >= 1f)
            {
                state.Flash = null;
            }
            else
            {
                frame.FlashLine = flash.Value.Line;
                frame.Flash = UiColor.Mix(theme.EditorBackground, palette.Flash, 1f - t);
                ui.RequestUpdate();
            }
        }
    }

    private static void Scroll(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, float rowHeight, float advance, float textLeft,
        UiRect content, float deltaY)
    {
        var folds = state.Folds;
        var viewport = content.Height;
        var pending = state.Pending;
        if (pending is PendingScroll.Restore restoring)
        {
            var restore = restoring.Position;
            if (viewport <= 0f)
            {
                ui.RequestUpdate();
            }
            else
            {
                state.Pending = null;
                var top = Math.Clamp(restore.TopLine, 0, Math.Max(0, document.Lines.Length - 1));
                state.RevealFolds(top);
                state.ScrollY = folds.RowOf(top).Top(rowHeight);
                state.ScrollX = restore.ScrollX;
                state.Selection.Select(CodeText.Clamp(document, restore.Anchor), CodeText.Clamp(document, restore.Caret));
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

        if (pending is PendingScroll.Reveal reveal)
        {
            if (viewport <= 0f)
            {
                ui.RequestUpdate();
                return;
            }
            var line = Math.Clamp(reveal.Line, 0, Math.Max(0, document.Lines.Length - 1));
            state.RevealFolds(line);
            state.ScrollY = MathF.Max(0f, folds.RowOf(line).Top(rowHeight) - viewport / 3f);
            state.Flash = new CodeFlash(line, ui.RealTime);
            state.Pending = null;
            ui.RequestUpdate();
        }
        else if (pending is PendingScroll.ShowMatch match)
        {
            if (viewport <= 0f || content.Width <= 0f)
            {
                ui.RequestUpdate();
                return;
            }
            state.RevealFolds(match.Line);
            var top = folds.RowOf(match.Line).Top(rowHeight);
            if (top < state.ScrollY || top + rowHeight > state.ScrollY + viewport)
                state.ScrollY = MathF.Max(0f, top - viewport / 3f);
            var visible = content.Width - textLeft - advance;
            var from = match.ColumnStart * advance;
            var to = match.ColumnEnd * advance;
            if (from < state.ScrollX)
                state.ScrollX = MathF.Max(0f, from - 4f * advance);
            else if (to > state.ScrollX + visible)
                state.ScrollX = to - visible + 4f * advance;
            state.Pending = null;
        }
    }

    /// <summary>The type or member named at <paramref name="at"/> or just before it, as the caret
    /// sits after a word as often as in it; punctuation linked to a member does not count.</summary>
    private static CodeReference.Entity? AnalyzableAt(DecompiledDocument document, TextPosition at)
    {
        if (at.Line < 0 || at.Line >= document.Lines.Length) return null;
        var spans = document.Lines[at.Line].Spans;
        foreach (var column in (ReadOnlySpan<int>)[at.Column, at.Column - 1])
        {
            if (column < 0)
                continue;
            var index = CodeText.SpanAt(spans, column);
            if (index == null)
                continue;
            var span = spans[index.Value];
            if (span.Kind != SpanKind.Plain && span.Reference is CodeReference.Entity entity)
                return entity;
        }
        return null;
    }

    private static void HandleKeys(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, int focusId, bool focused, float advance)
    {
        var keys = ui.Input.Keys;
        for (var k = 0; k < keys.Count; k++)
        {
            if (RunShortcut(ui, state, document, focused, k))
                continue;
            switch (keys[k].Key)
            {
                case KeyboardKey.Escape when state.FindOpen:
                    state.FindOpen = false;
                    ui.Focus(focusId, takesText: false);
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

    /// <summary>Runs the code's shortcut the key at <paramref name="index"/> completes, if any; false
    /// for a key that is none, which then moves the view.</summary>
    private static bool RunShortcut(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, bool focused, int index)
    {
        if (ui.IsPressedAt(Shortcuts.Find, index))
        {
            if (!state.FindOpen && state.HasSelection && state.Selection.Anchor.Line == state.Selection.Caret.Line)
                state.Query = state.SelectedText;
            state.FindOpen = true;
            state.FocusFind = true;
            return true;
        }
        if (state.Query.Length > 0 && (ui.IsPressedAt(Shortcuts.FindNext, index) || ui.IsPressedAt(Shortcuts.FindPrevious, index)))
        {
            state.FindOpen = true;
            Step(state, document, ui.IsPressedAt(Shortcuts.FindPrevious, index) ? -1 : 1);
            return true;
        }
        if (!focused)
            return false;
        if (ui.IsPressedAt(Shortcuts.ToggleFold, index))
        {
            state.ToggleFold(state.Selection.Caret.Line);
            return true;
        }
        if (ui.IsPressedAt(Shortcuts.ToggleAllFolds, index))
        {
            if (state.Folds.AnyCollapsed) state.ExpandAll();
            else state.CollapseAll();
            return true;
        }
        if (ui.IsPressedAt(Shortcuts.CollapseFold, index))
        {
            FoldAtCaret(state, collapse: true);
            return true;
        }
        if (ui.IsPressedAt(Shortcuts.ExpandFold, index))
        {
            FoldAtCaret(state, collapse: false);
            return true;
        }
        if (ui.IsPressedAt(Shortcuts.SelectAll, index))
        {
            state.Selection.Select(default, CodeText.End(document));
            return true;
        }
        if (state.HasSelection && ui.IsPressedAt(Shortcuts.CopyPlainText, index))
        {
            ui.ClipboardText = state.SelectedText;
            return true;
        }
        if (state.HasSelection && ui.IsPressedAt(Shortcuts.Copy, index))
        {
            CopySelection(ui, state);
            return true;
        }
        return false;
    }

    private static void FoldAtCaret(CodeViewState state, bool collapse)
    {
        var fold = state.Folds.Innermost(state.Selection.Caret.Line, collapsed: !collapse);
        if (fold != null)
            state.SetCollapsed(fold.Value, collapse);
    }

    /// <summary>Copies the selection as plain text and as HTML in the colors and font it is drawn in.</summary>
    private static void CopySelection(Ui<UiState> ui, CodeViewState state)
    {
        var style = CopyStyleOf(ui, state.Frame);
        ui.SetClipboardHtml(state.SelectedHtml(style), state.SelectedText);
    }

    private static CodeHtmlStyle CopyStyleOf(Ui<UiState> ui, CodeViewFrame frame)
    {
        var family = CodeFontChoices.NameOf(ui.State.Display.Font);
        return new CodeHtmlStyle(frame.Palette, frame.Background, frame.ErrorText, family, frame.FontSize, frame.RowHeight);
    }

    private static void FindBar(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, int focusId, int findId)
    {
        var t = ui.State.Strings;
        var theme = ui.Theme;
        using (ui.PaneToolbar())
        {
            if (state.FocusFind)
            {
                ui.Focus(findId);
                state.FocusFind = false;
            }

            var submitted = ui.TextField("find", ref state.Query, t.FindPlaceholder, Sizing.Grow(AppLayout.FindMinWidth, AppLayout.FindWidth));
            var queryChanged = !string.Equals(state.Query, state.Find.Query, StringComparison.Ordinal);
            if (state.Find.Update(document, state.Query))
            {
                state.CurrentMatch = queryChanged ? state.Find.FirstFrom(state.Selection.Start) : null;
                ShowMatch(state);
            }
            if (submitted)
                Step(state, document, (ui.Input.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);

            var count = state.Find.Matches.Count;
            var label = state.Query.Length == 0 ? ""
                : count == 0 ? t.FindNoResults
                : ui.Memo("count", (state.CurrentMatch, count, t), static a => a.CurrentMatch != null ? a.t.FindPosition(a.CurrentMatch.Value + 1, a.count) : a.t.FindMatches(a.count));
            ui.Text(label, new Box { Width = Sizing.Grow() }, color: count == 0 && state.Query.Length > 0 ? theme.Warning : theme.TextMuted,
                size: theme.FontSizeSmall, overflow: TextOverflow.Ellipsis);
            if (ui.IconButton("previous", ui.State.Icons.CodeFindPrevious, ui.Memo("previous-tip", t, static s => s.FindPrevious("Shift+F3")), count > 0))
                Step(state, document, -1);
            if (ui.IconButton("next", ui.State.Icons.CodeFindNext, ui.Memo("next-tip", t, static s => s.FindNext("F3")), count > 0))
                Step(state, document, 1);
            if (ui.IconButton("close", ui.State.Icons.CodeFindClose, ui.Memo("close-tip", t, static s => s.FindClose("Esc"))))
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
            state.CurrentMatch = null;
            return;
        }
        var current = state.CurrentMatch;
        if (current != null)
            state.CurrentMatch = (current.Value + direction + count) % count;
        else if (direction > 0)
            state.CurrentMatch = state.Find.FirstFrom(state.Selection.Caret);
        else
            state.CurrentMatch = (state.Find.FirstOnOrAfter(state.Selection.Caret) - 1 + count) % count;
        ShowMatch(state);
    }

    private static void ShowMatch(CodeViewState state)
    {
        var current = state.CurrentMatch;
        if (current == null) return;
        var match = state.Find.Matches[current.Value];
        state.Selection.Select(match, match with { Column = match.Column + state.Find.Length });
        state.RevealFolds(match.Line);
        state.Pending = new PendingScroll.ShowMatch(match.Line, match.Column, match.Column + state.Find.Length);
    }

    /// <summary>The items that show <paramref name="entity"/> on a diagram, when it is a type.</summary>
    private static CodeViewIntent? DiagramItems(Ui<UiState> ui, CodeReference.Entity entity)
    {
        if (entity.Kind != EntityKind.Type)
            return null;
        var t = ui.State.Strings;
        CodeViewIntent? picked = null;
        if (ui.MenuItem(t.TreeShowInDiagram, id: "show-in-diagram"))
            picked = new CodeViewIntent.ShowInDiagram(entity);
        if (ui.MenuItem(t.TreeShowInNewDiagram, id: "show-in-new-diagram"))
            picked = new CodeViewIntent.ShowInNewDiagram(entity);
        return picked;
    }

    private static CodeViewIntent? ContextMenu(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, bool bookmarkable)
    {
        var t = ui.State.Strings;
        CodeViewIntent? picked = null;
        using var menu = ui.ContextMenu("menu");
        if (!menu.IsOpen) return picked;
        var target = state.Menu;

        if (ui.MenuItem(t.CodeCopy, Shortcuts.Copy, enabled: state.HasSelection, id: "copy"))
            CopySelection(ui, state);
        if (ui.MenuItem(t.CodeCopyPlainText, Shortcuts.CopyPlainText, enabled: state.HasSelection, id: "copy-plain-text"))
            ui.ClipboardText = state.SelectedText;
        if (ui.MenuItem(t.CodeSelectAll, Shortcuts.SelectAll, id: "select-all"))
            state.Selection.Select(default, CodeText.End(document));
        var line = target.Line;
        if (bookmarkable && line != null)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.CodeToggleBookmark, id: "toggle-bookmark",
                    icon: new UiIcon(ui.State.Icons.CodeToggleBookmark, ui.State.CodeViewPalette.Bookmark)))
                picked = new CodeViewIntent.ToggleBookmark(line.Value);
        }
        if (document.Folds.Length > 0)
        {
            ui.MenuSeparator();
            var fold = line != null ? state.Folds.Innermost(line.Value) : null;
            if (ui.MenuItem(t.CodeToggleFold, Shortcuts.ToggleFold, id: "toggle-fold", enabled: fold != null) && line != null)
                state.ToggleFold(line.Value);
            if (ui.MenuItem(t.CodeCollapseAll, id: "collapse-all"))
                state.CollapseAll();
            if (ui.MenuItem(t.CodeExpandAll, id: "expand-all"))
                state.ExpandAll();
        }
        var reference = target.Navigable;
        if (reference != null)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.CodeGoToDefinition, id: "go-to-definition"))
                picked = new CodeViewIntent.Navigate(reference, false);
            if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
                picked = new CodeViewIntent.Navigate(reference, true);
        }
        var entity = target.Entity;
        if (entity != null)
        {
            if (target.Navigable == null)
                ui.MenuSeparator();
            if (ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
                picked = new CodeViewIntent.Analyze(entity);
            if (ui.MenuItem(t.TreeShowInApiReference, id: "show-in-api-reference", icon: new UiIcon(ui.State.Icons.LinkApiReference)))
                picked = new CodeViewIntent.ShowInApiReference(entity);
            picked = DiagramItems(ui, entity) ?? picked;
        }
        var shown = target.Metadata;
        if (shown != null)
        {
            if (target.Navigable == null && target.Entity == null)
                ui.MenuSeparator();
            if (ui.MenuItem(t.TreeShowInMetadata, id: "show-in-metadata"))
                picked = new CodeViewIntent.ShowInMetadata(shown);
        }
        return picked;
    }
}
