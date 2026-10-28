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

/// <summary>Everything a <see cref="CodeViewWidget.CodeView"/> keeps between frames: scroll,
/// selection, folds, find. One per open tab, owned by the app.</summary>
public sealed class CodeViewState
{
    internal const int NoLine = -1;

    internal readonly CodeViewFrame Frame = new();
    internal readonly CodeFindIndex Find = new();
    internal readonly CodeFolds Folds = new();
    internal readonly MatchingBrackets Brackets = new();

    internal DecompiledDocument? Document;
    internal int MaxColumns;
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
    internal CodeReference? MenuEntity;
    internal CodeReference? MenuMetadata;
    internal CodeTooltip? Tooltip;
    internal int TooltipPart;
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

    public CodeViewState() => Selection = new CodeSelection(Folds);

    public CodeSelection Selection { get; }

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
            return new CodeViewPosition(top, ScrollX, Selection.Anchor, Selection.Caret);
        }
    }

    /// <summary>Returns the view to <paramref name="position"/> once the next document is shown —
    /// instead of revealing a line.</summary>
    public void Restore(CodeViewPosition position)
    {
        PendingRestore = position;
        PendingReveal = NoLine;
    }

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
        Selection.Select(default, default);
        PressedAt = default;
        PressedSpanLine = PressedSpan = NoLine;
        PressedOnFold = false;
        ScrollX = ScrollY = LaidOutScrollX = LaidOutScrollY = 0f;
        Marked = MenuReference = null;
        MenuMetadata = null;
        Tooltip = null;
        TooltipPart = 0;
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

/// <summary>How the user asked a code view to change the size of its code.</summary>
public enum CodeZoom
{
    None,
    In,
    Out,
    Reset,
}

public readonly record struct CodeViewResult(CodeReference? Navigate, bool NavigateInNewTab, int? ToggleBookmark = null, CodeReference? Analyze = null,
    CodeZoom Zoom = CodeZoom.None, CodeReference? ShowInMetadata = null, CodeReference? ShowInApiReference = null,
    CodeReference? ShowInDiagram = null, CodeReference? ShowInNewDiagram = null);

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
        var result = default(CodeViewResult);
        var zoom = CodeZoom.None;

        if (focused || findFocused)
        {
            HandleKeys(ui, state, document, focusId, focused, advance);
            if (focused && Shortcuts.AnalyzePressed(ui) && AnalyzableAt(document, state.Selection.Caret) is { } analyzed)
                result = result with { Analyze = analyzed };
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
            var hoverMark = CodeViewState.NoLine;
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
                    else if (bookmarks != null && pointer.X < rect.X + markWidth)
                    {
                        hoverMark = line;
                    }
                }
            }
            var hoverReference = hoverSpan >= 0 ? document.Lines[hoverLine].Spans[hoverSpan].Reference : null;
            var hoverNavigable = hoverReference is { IsNavigable: true } && !document.Lines[hoverLine].Spans[hoverSpan].IsDefinition;
            var hoverToggle = hoverFold >= 0 || hoverChip >= 0 || hoverMark >= 0;

            if (i.Hovered || i.Held)
                ui.SetCursor((hoverNavigable || hoverToggle) && !state.PressMoved ? MouseCursor.Hand : MouseCursor.Text);

            var scrollDeltaY = 0f;
            if (i.Pressed && hasRect)
            {
                ui.Focus(focusId, takesText: false);
                state.PressMoved = false;
                state.PressedOnFold = hoverToggle;
                state.PressedSpanLine = state.PressedSpan = CodeViewState.NoLine;
                if (hoverMark >= 0)
                {
                    result = result with { ToggleBookmark = hoverMark };
                }
                else if (hoverFold >= 0)
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
                    state.Selection.Press(document, at, i.ClickCount, (ui.Input.Modifiers & KeyModifiers.Shift) != 0);
                }
            }
            else if (i.Held && hasRect && !state.PressedOnFold)
            {
                var at = PositionAt(pointer.X, pointer.Y);
                if (!state.PressMoved && at != state.PressedAt)
                    state.PressMoved = true;
                if (state.PressMoved)
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

            if (i.Clicked && !state.PressMoved && !state.PressedOnFold && state.Selection.Clicks == 1 && (ui.Input.Modifiers & KeyModifiers.Shift) == 0)
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
                state.MenuEntity = hoverReference is { IsNavigable: true, Kind: not CodeReferenceKind.Assembly } ? hoverReference : null;
                state.MenuMetadata = hoverReference is { IsNavigable: true } ? hoverReference : null;
                state.MenuLine = hasRect ? PositionAt(pointer.X, pointer.Y).Line : CodeViewState.NoLine;
                ui.OpenContextMenu("menu");
            }

            Scroll(ui, state, document, rowHeight, advance, textLeft, content, scrollDeltaY);
            FillFrame(ui, state, document, theme, palette, cell, rowHeight, markWidth, foldLeft, foldWidth, gutterWidth, textLeft, digits,
                focused || findFocused, bookmarks, hoverLine, hoverSpan, hoverReference, hoverNavigable, hoverFold, hoverChip, hoverMark);
            FillDisplayAids(state, document, shown, fontSize);

            using (ui.Box(new Box { Width = Sizing.Grow(), Height = Sizing.Grow(), Gap = 0f }))
            {
                using (var list = ui.VirtualScroll(codeId, folds.RowCount, rowHeight, ref state.ScrollX, ref state.ScrollY,
                           new Box { Background = theme.EditorBackground }))
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
            if (tooltips != null && hasRect)
            {
                var hovered = new HoveredSpan(hoverLine, hoverSpan, rect.X + textLeft, rect.Y);
                var followed = ReferenceTooltip(ui, state, document, tooltips, codeId, hovered, advance, rowHeight);
                if (followed.Navigate != null)
                    result = followed;
            }
            state.LaidOutScrollX = state.ScrollX;
            state.LaidOutScrollY = state.ScrollY;
        }

        var picked = ContextMenu(ui, state, document, bookmarks != null);
        if (picked.Navigate != null)
            result = picked;
        if (picked.ToggleBookmark != null)
            result = result with { ToggleBookmark = picked.ToggleBookmark };
        if (picked.Analyze != null)
            result = result with { Analyze = picked.Analyze };
        if (picked.ShowInMetadata != null)
            result = result with { ShowInMetadata = picked.ShowInMetadata };
        if (picked.ShowInApiReference != null)
            result = result with { ShowInApiReference = picked.ShowInApiReference };
        if (picked.ShowInDiagram != null)
            result = result with { ShowInDiagram = picked.ShowInDiagram };
        if (picked.ShowInNewDiagram != null)
            result = result with { ShowInNewDiagram = picked.ShowInNewDiagram };
        return result with { Zoom = zoom };
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
        foreach (var stroke in ui.Input.Keys)
        {
            if (!IsShortcut(stroke.Modifiers))
                continue;
            var zoom = ZoomOfKey(stroke.Key);
            if (zoom != CodeZoom.None)
                return zoom;
        }
        return CodeZoom.None;
    }

    private static CodeZoom ZoomOfKey(KeyboardKey key) => key switch
    {
        KeyboardKey.Equals => CodeZoom.In,
        KeyboardKey.NumpadAdd => CodeZoom.In,
        KeyboardKey.Minus => CodeZoom.Out,
        KeyboardKey.NumpadSubtract => CodeZoom.Out,
        KeyboardKey.Alpha0 => CodeZoom.Reset,
        KeyboardKey.Numpad0 => CodeZoom.Reset,
        _ => CodeZoom.None,
    };

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

    private static bool IsShortcut(KeyModifiers modifiers) => (modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;

    /// <summary>Fills what <paramref name="display"/> draws around the text: the caret's line and the
    /// brackets matching at the caret.</summary>
    private static void FillDisplayAids(CodeViewState state, DecompiledDocument document, DisplayOptions display, float fontSize)
    {
        var frame = state.Frame;
        var caret = state.Selection.Caret;
        frame.FontSize = fontSize;
        frame.CurrentLine = display.HighlightCurrentLine ? caret.Line : CodeViewState.NoLine;
        frame.Brackets = FindBracketsToShow(state, document, display, caret);
    }

    private static BracketPair? FindBracketsToShow(CodeViewState state, DecompiledDocument document, DisplayOptions display, TextPosition caret)
    {
        if (!display.HighlightMatchingBrackets)
            return null;
        return state.Brackets.Find(document, caret);
    }

    /// <summary>The span under the pointer and where the view's text starts on screen.</summary>
    private readonly record struct HoveredSpan(int Line, int Span, float TextX, float TextY);

    /// <summary>The tooltip of the type or member under the pointer, under its span once it has
    /// been looked up.</summary>
    private static CodeViewResult ReferenceTooltip(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, CodeTooltips tooltips,
        int codeId, HoveredSpan hovered, float advance, float rowHeight)
    {
        var tooltip = TooltipOf(state, document, tooltips, hovered);
        var part = 0;
        if (tooltip != null)
            part = HashCode.Combine(hovered.Line, hovered.Span) | 1;
        var anchor = TooltipAnchor(state, document, hovered, advance, rowHeight);
        using var popup = ui.TooltipContent(codeId, part, anchor, out var shownPart, CodeTooltipView.Look);
        RememberTooltip(state, tooltip, part, shownPart);
        if (!popup.IsOpen)
            return default;
        if (state.TooltipPart != shownPart)
            return default;
        var click = CodeTooltipView.Render(ui, state.Tooltip!);
        return FollowLink(ui, click);
    }

    /// <summary>Keeps the tooltip of the part under the pointer, unless another part's tooltip is
    /// still shown while the pointer crosses over to it.</summary>
    private static void RememberTooltip(CodeViewState state, CodeTooltip? tooltip, int part, int shownPart)
    {
        if (tooltip == null)
            return;
        if (shownPart != 0 && shownPart != part)
            return;
        state.Tooltip = tooltip;
        state.TooltipPart = part;
    }

    /// <summary>A link clicked in the tooltip asks to navigate to it, in a new tab with a middle
    /// click or Ctrl, and closes the tooltip.</summary>
    private static CodeViewResult FollowLink(Ui<UiState> ui, RichTextClick click)
    {
        if (click.Link is not CodeReference reference)
            return default;
        ui.HideTooltip();
        var withModifier = (ui.Input.Modifiers & (KeyModifiers.Control | KeyModifiers.Super)) != 0;
        var inNewTab = click.Button == PointerButton.Middle || withModifier;
        return new CodeViewResult(reference, inNewTab);
    }

    /// <summary>The tooltip of the span under the pointer when it points at a type or member and
    /// its lookup is done; null otherwise, and while a press drags a selection.</summary>
    private static CodeTooltip? TooltipOf(CodeViewState state, DecompiledDocument document, CodeTooltips tooltips, HoveredSpan hovered)
    {
        if (hovered.Span < 0)
            return null;
        if (state.PressMoved)
            return null;
        var reference = document.Lines[hovered.Line].Spans[hovered.Span].Reference;
        if (!HasTooltip(reference))
            return null;
        tooltips.TryGet(reference!, out var tooltip);
        return tooltip;
    }

    private static bool HasTooltip(CodeReference? reference) =>
        reference is { IsNavigable: true } && reference.Kind != CodeReferenceKind.Assembly;

    /// <summary>The hovered span, widened to the left by the tooltip's padding so the tooltip's
    /// text starts where the word does.</summary>
    private static UiRect TooltipAnchor(CodeViewState state, DecompiledDocument document, HoveredSpan hovered, float advance, float rowHeight)
    {
        var span = SpanRect(state, document, hovered, advance, rowHeight);
        return span with { X = span.X - CodeTooltipView.PaddingX, Width = span.Width + CodeTooltipView.PaddingX };
    }

    private static UiRect SpanRect(CodeViewState state, DecompiledDocument document, HoveredSpan hovered, float advance, float rowHeight)
    {
        if (hovered.Span < 0)
            return default;
        var span = document.Lines[hovered.Line].Spans[hovered.Span];
        var x = hovered.TextX - state.LaidOutScrollX + span.Start * advance;
        var y = hovered.TextY + state.Folds.RowOf(hovered.Line) * rowHeight - state.LaidOutScrollY;
        return new UiRect(x, y, span.Length * advance, rowHeight);
    }

    private static float AutoScroll(float distance, float unit, float deltaTime) =>
        distance == 0f ? 0f : (distance * AutoScrollSpeed + MathF.Sign(distance) * unit * AutoScrollSpeed) * deltaTime;

    private static void FillFrame(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, UiTheme theme, CodeViewPalette palette, CellMetrics cell,
        float rowHeight, float markWidth, float foldLeft, float foldWidth, float gutterWidth, float textLeft, int digits, bool active,
        IReadOnlyDictionary<int, bool>? bookmarks, int hoverLine, int hoverSpan, CodeReference? hoverReference, bool hoverNavigable, int hoverFold,
        int hoverChip, int hoverMark)
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
        frame.HoverMark = hoverMark;
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
                ui.RequestUpdate();
            }
        }
    }

    private static void Scroll(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, float rowHeight, float advance, float textLeft,
        UiRect content, float deltaY)
    {
        var folds = state.Folds;
        var viewport = content.Height;
        if (state.PendingRestore is { } restore)
        {
            if (viewport <= 0f)
            {
                ui.RequestUpdate();
            }
            else
            {
                state.PendingRestore = null;
                var top = Math.Clamp(restore.TopLine, 0, Math.Max(0, document.Lines.Length - 1));
                state.RevealFolds(top);
                state.ScrollY = folds.RowOf(top) * rowHeight;
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

        if (state.PendingReveal != CodeViewState.NoLine || state.PendingShowLine != CodeViewState.NoLine)
        {
            if (viewport <= 0f)
            {
                ui.RequestUpdate();
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
                ui.RequestUpdate();
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

    /// <summary>The type or member named at <paramref name="at"/> or just before it, as the caret
    /// sits after a word as often as in it; punctuation linked to a member does not count.</summary>
    private static CodeReference? AnalyzableAt(DecompiledDocument document, TextPosition at)
    {
        if (at.Line < 0 || at.Line >= document.Lines.Length) return null;
        var spans = document.Lines[at.Line].Spans;
        foreach (var column in (ReadOnlySpan<int>)[at.Column, at.Column - 1])
        {
            if (column >= 0 && CodeText.SpanAt(spans, column) is >= 0 and var index
                && spans[index] is { Kind: not SpanKind.Plain, Reference: { IsNavigable: true, Kind: not CodeReferenceKind.Assembly } reference })
                return reference;
        }
        return null;
    }

    private static void HandleKeys(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, int focusId, bool focused, float advance)
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
                    if (!state.FindOpen && state.HasSelection && state.Selection.Anchor.Line == state.Selection.Caret.Line)
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
                    state.ToggleFold(state.Selection.Caret.Line);
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
                    state.Selection.Select(default, CodeText.End(document));
                    break;
                case KeyboardKey.C when shortcut && shift && focused && state.HasSelection:
                    ui.ClipboardText = state.SelectedText;
                    break;
                case KeyboardKey.C when shortcut && focused && state.HasSelection:
                    CopySelection(ui, state);
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
        var fold = state.Folds.Innermost(state.Selection.Caret.Line, collapsed: !collapse);
        if (fold >= 0)
            state.SetCollapsed(fold, collapse);
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
                state.CurrentMatch = queryChanged ? state.Find.FirstFrom(state.Selection.Start) : CodeViewState.NoLine;
                ShowMatch(state);
            }
            if (submitted)
                Step(state, document, (ui.Input.Modifiers & KeyModifiers.Shift) != 0 ? -1 : 1);

            var count = state.Find.Matches.Count;
            var label = state.Query.Length == 0 ? ""
                : count == 0 ? t.FindNoResults
                : ui.Memo("count", (state.CurrentMatch, count, t), static a => a.CurrentMatch >= 0 ? a.t.FindPosition(a.CurrentMatch + 1, a.count) : a.t.FindMatches(a.count));
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
            state.CurrentMatch = CodeViewState.NoLine;
            return;
        }
        state.CurrentMatch = state.CurrentMatch < 0
            ? direction > 0 ? state.Find.FirstFrom(state.Selection.Caret) : (state.Find.FirstOnOrAfter(state.Selection.Caret) - 1 + count) % count
            : (state.CurrentMatch + direction + count) % count;
        ShowMatch(state);
    }

    private static void ShowMatch(CodeViewState state)
    {
        if (state.CurrentMatch < 0) return;
        var match = state.Find.Matches[state.CurrentMatch];
        state.Selection.Select(match, match with { Column = match.Column + state.Find.Length });
        state.RevealFolds(match.Line);
        state.PendingShowLine = match.Line;
        state.ShowColumns(match.Column, match.Column + state.Find.Length);
    }

    /// <summary>The items that show <paramref name="entity"/> on a diagram, when it is a type.</summary>
    private static CodeViewResult DiagramItems(Ui<UiState> ui, CodeReference entity, CodeViewResult result)
    {
        if (entity.Kind != CodeReferenceKind.Type)
            return result;
        var t = ui.State.Strings;
        if (ui.MenuItem(t.TreeShowInDiagram, id: "show-in-diagram"))
            result = result with { ShowInDiagram = entity };
        if (ui.MenuItem(t.TreeShowInNewDiagram, id: "show-in-new-diagram"))
            result = result with { ShowInNewDiagram = entity };
        return result;
    }

    private static CodeViewResult ContextMenu(Ui<UiState> ui, CodeViewState state, DecompiledDocument document, bool bookmarkable)
    {
        var t = ui.State.Strings;
        var result = default(CodeViewResult);
        using var menu = ui.ContextMenu("menu");
        if (!menu.IsOpen) return result;

        if (ui.MenuItem(t.CodeCopy, "Ctrl+C", enabled: state.HasSelection, id: "copy"))
            CopySelection(ui, state);
        if (ui.MenuItem(t.CodeCopyPlainText, "Ctrl+Shift+C", enabled: state.HasSelection, id: "copy-plain-text"))
            ui.ClipboardText = state.SelectedText;
        if (ui.MenuItem(t.CodeSelectAll, "Ctrl+A", id: "select-all"))
            state.Selection.Select(default, CodeText.End(document));
        if (bookmarkable && state.MenuLine >= 0)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.CodeToggleBookmark, id: "toggle-bookmark",
                    icon: new UiIcon(ui.State.Icons.CodeToggleBookmark, ui.State.CodeViewPalette.Bookmark)))
                result = result with { ToggleBookmark = state.MenuLine };
        }
        if (document.Folds.Length > 0)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.CodeToggleFold, "Ctrl+M, M", id: "toggle-fold", enabled: state.MenuLine >= 0 && state.Folds.Innermost(state.MenuLine) >= 0))
                state.ToggleFold(state.MenuLine);
            if (ui.MenuItem(t.CodeCollapseAll, id: "collapse-all"))
                state.CollapseAll();
            if (ui.MenuItem(t.CodeExpandAll, id: "expand-all"))
                state.ExpandAll();
        }
        if (state.MenuReference is { } reference)
        {
            ui.MenuSeparator();
            if (ui.MenuItem(t.CodeGoToDefinition, id: "go-to-definition"))
                result = new CodeViewResult(reference, false);
            if (ui.MenuItem(t.CodeOpenInNewTab, id: "open-in-new-tab"))
                result = new CodeViewResult(reference, true);
        }
        if (state.MenuEntity is { } entity)
        {
            if (state.MenuReference == null)
                ui.MenuSeparator();
            if (ui.MenuItem(t.TreeAnalyze, Shortcuts.Analyze, id: "analyze"))
                result = result with { Analyze = entity };
            if (ui.MenuItem(t.TreeShowInApiReference, id: "show-in-api-reference", icon: new UiIcon(ui.State.Icons.LinkApiReference)))
                result = result with { ShowInApiReference = entity };
            result = DiagramItems(ui, entity, result);
        }
        if (state.MenuMetadata is { } shown)
        {
            if (state.MenuReference == null && state.MenuEntity == null)
                ui.MenuSeparator();
            if (ui.MenuItem(t.TreeShowInMetadata, id: "show-in-metadata"))
                result = result with { ShowInMetadata = shown };
        }
        return result;
    }
}
