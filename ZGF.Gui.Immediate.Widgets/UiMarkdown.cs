using ZGF.Desktop;
using ZGF.KeyboardModule;
using ZGF.Markdown;

namespace ZGF.Gui.Immediate.Widgets;

/// <summary>How <see cref="UiWidgets.Markdown"/> draws what the theme leaves to the app: the
/// families code and italic text are drawn in, registered by the app, or null for the theme's font,
/// which leaves italic text upright; and the icons of a code block's button that copies its code,
/// which is left out without <see cref="CopyIcon"/>, and of the moment after it did.</summary>
public readonly record struct MarkdownLook(
    string? CodeFamily = null,
    string? ItalicFamily = null,
    IconGlyph? CopyIcon = null,
    IconGlyph? CopiedIcon = null);

public static partial class UiWidgets
{
    public const string CopyCodeTooltip = "Copy code";
    public const string CopiedCodeTooltip = "Copied";

    private const int MarkdownSalt = 0x4D44574E;
    private const int MarkdownFocusSalt = 0x4D444643;
    private const int MarkdownLeafSalt = 0x4D444C46;
    private const int MarkdownScrollSalt = 0x4D445343;
    private const float QuoteBarWidth = 3f;
    private const double CopiedSeconds = 1.5;

    /// <summary>
    /// <paramref name="text"/> as markdown, filling the width it is given: paragraphs wrapped,
    /// headings, fenced code on a panel that scrolls sideways, bulleted, numbered and task lists,
    /// quotes, rules and tables. Unsupported syntax shows as the text it is. Bold, italic, code,
    /// strikethrough and links style the text within a block; a link underlines under the pointer
    /// and a left or middle click on it is returned, its <see cref="RichTextClick.Link"/> the URL.
    /// <para>
    /// Its text selects as in a text field — dragging across blocks, double-clicking a word,
    /// triple-clicking a block, shift-clicking — and Ctrl+C copies what is selected as it reads,
    /// without the markup, while the selection holds focus; Ctrl+A selects it all. A press elsewhere
    /// that takes focus clears it. With a <see cref="MarkdownLook.CopyIcon"/>, each code block has a
    /// button that copies its code.
    /// </para>
    /// The text is parsed again only when it changes, and the blocks it still begins with keep their
    /// layouts, so a reply streaming in costs the blocks that grew.
    /// </summary>
    public static RichTextClick Markdown(this Ui ui, string id, string text, MarkdownLook look = default) =>
        ShowDocument(ui, id, text, look, plain: false);

    /// <summary><paramref name="text"/> as it is, wrapped to the width it is given and breaking at
    /// its line breaks, selecting and copying as <see cref="Markdown"/> does.</summary>
    public static void SelectableParagraph(this Ui ui, string id, string text) =>
        ShowDocument(ui, id, text, default, plain: true);

    private static RichTextClick ShowDocument(Ui ui, string id, string text, MarkdownLook look, bool plain)
    {
        using var scope = ui.Id(id);
        var state = ui.GetState<MarkdownState>(ui.GetId(MarkdownSalt));
        state.Refresh(ui, text, look, plain);
        var drawing = new MarkdownDrawing(ui, state, look);
        drawing.FollowFocus();
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = ui.Theme.Gap }))
            drawing.Blocks(state.Shown);
        drawing.FollowDrag();
        return drawing.Clicked;
    }

    private sealed class MarkdownState
    {
        private string? _text;
        private UiTheme? _theme;
        private MarkdownLook _look;
        private bool _plain;
        private IReadOnlyList<MarkdownBlock> _sources = [];
        private int[] _leafEnds = [];

        public ShownBlock[] Shown { get; private set; } = [];
        public List<MarkdownLeaf> Leaves { get; } = [];
        public MarkdownSelection Selection { get; } = new();

        /// <summary>The code block whose code was last copied, and when.</summary>
        public int CopiedLeaf = -1;
        public double CopiedAt;

        /// <summary>Parses <paramref name="text"/> again when it changed since the last frame, keeping
        /// the blocks it still begins with and their leaves, and a selection within them; and builds
        /// it again in full when the theme or the look changed.</summary>
        public void Refresh(Ui ui, string text, MarkdownLook look, bool plain)
        {
            var sameLook = ui.Theme.Equals(_theme) && _look == look && _plain == plain;
            if (sameLook && _text == text)
                return;
            var parsed = plain ? [PlainParagraph(text)] : new BasicMarkdownParser().Parse(text).Blocks;
            var kept = 0;
            while (sameLook && kept < parsed.Count && kept < _sources.Count && _sources[kept].Equals(parsed[kept]))
                kept++;
            var keptLeaves = kept > 0 ? _leafEnds[kept - 1] : 0;
            Leaves.RemoveRange(keptLeaves, Leaves.Count - keptLeaves);
            if (Selection.End.Leaf >= keptLeaves)
                Selection.Clear();

            var shown = new ShownBlock[parsed.Count];
            var leafEnds = new int[parsed.Count];
            for (var i = 0; i < parsed.Count; i++)
            {
                shown[i] = i < kept ? Shown[i] : ShownMarkdown.Build(ui, look, parsed[i], ui.Theme.Text, Leaves);
                leafEnds[i] = i < kept ? _leafEnds[i] : Leaves.Count;
            }
            _text = text;
            _theme = ui.Theme;
            _look = look;
            _plain = plain;
            _sources = parsed;
            _leafEnds = leafEnds;
            Shown = shown;
        }

        private static ParagraphBlock PlainParagraph(string text) => new([new InlineRun(text)]);
    }

    /// <summary>Records shown blocks and takes the pointer and keys that select and copy their text,
    /// keeping the link clicked among them.</summary>
    private sealed class MarkdownDrawing(Ui ui, MarkdownState state, MarkdownLook look)
    {
        private readonly int _focusId = ui.GetId(MarkdownFocusSalt);
        private bool _held;
        private bool _pressed;

        public RichTextClick Clicked { get; private set; }

        private MarkdownSelection Selection => state.Selection;
        private List<MarkdownLeaf> Leaves => state.Leaves;
        private uint CodeChip => ui.Theme.Control;

        private int LeafId(int leaf) => ui.GetId(HashCode.Combine(MarkdownLeafSalt, leaf));

        /// <summary>Clears the selection once something else took focus, and while it holds focus takes
        /// Ctrl+C, which copies the selection, and Ctrl+A, which selects everything.</summary>
        public void FollowFocus()
        {
            if (!ui.HasFocus(_focusId))
            {
                if (Selection.HasSelection)
                    Selection.Clear();
                return;
            }
            var keys = ui.Input.Keys;
            for (var k = 0; k < keys.Count;)
            {
                var stroke = keys[k];
                if (TakeKey(stroke))
                    ui.Input.TakeKeyAt(k);
                else
                    k++;
            }
        }

        private bool TakeKey(KeyStroke stroke)
        {
            if (!IsCommand(stroke.Modifiers))
                return false;
            switch (stroke.Key)
            {
                case KeyboardKey.C when Selection.HasSelection:
                    ui.ClipboardText = Selection.Of(Leaves);
                    return true;
                case KeyboardKey.A:
                    Selection.SelectAll(Leaves);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>While a press that began on a leaf is held, extends the selection to the character
        /// nearest the pointer, in whichever leaf is nearest it.</summary>
        public void FollowDrag()
        {
            if (!_held || _pressed)
                return;
            if (PositionNearPointer() is not { } at)
                return;
            Selection.Drag(Leaves, at);
            ui.RequestUpdate();
        }

        private MarkdownPos? PositionNearPointer()
        {
            var pointer = ui.Input.Pointer;
            var nearest = -1;
            var best = float.MaxValue;
            for (var leaf = 0; leaf < Leaves.Count; leaf++)
            {
                if (!ui.TryGetContentRect(LeafId(leaf), out var rect))
                    continue;
                var dx = MathF.Max(0f, MathF.Max(rect.X - pointer.X, pointer.X - rect.Right));
                var dy = MathF.Max(0f, MathF.Max(rect.Y - pointer.Y, pointer.Y - rect.Bottom));
                var distance = dx * dx + dy * dy;
                if (distance >= best)
                    continue;
                best = distance;
                nearest = leaf;
            }
            if (nearest < 0)
                return null;
            return PositionIn(nearest);
        }

        private MarkdownPos? PositionIn(int leaf)
        {
            var shown = Leaves[leaf];
            var pointer = ui.Input.Pointer;
            var at = ui.RichTextCharAt(LeafId(leaf), shown.Runs, pointer.X, pointer.Y, shown.Wrap);
            if (at < 0)
                return null;
            return new MarkdownPos(leaf, at);
        }

        public void Blocks(IReadOnlyList<ShownBlock> blocks)
        {
            foreach (var block in blocks)
                Block(block);
        }

        private void Block(ShownBlock block)
        {
            switch (block)
            {
                case ShownBlock.Text text:
                    Leaf(text.Leaf);
                    break;
                case ShownBlock.Code code:
                    Code(code);
                    break;
                case ShownBlock.List list:
                    List(list);
                    break;
                case ShownBlock.Quote quote:
                    Quote(quote);
                    break;
                case ShownBlock.Break:
                    ui.Separator();
                    break;
                case ShownBlock.Table table:
                    Table(table);
                    break;
                default:
                    throw new InvalidOperationException($"No way to draw a {block.GetType().Name}.");
            }
        }

        /// <summary>A leaf's text with its part of the selection, which a press on it starts or, with
        /// Shift, extends. The pointer over it is a text cursor, and a hand over a link.</summary>
        private void Leaf(int leaf)
        {
            var shown = Leaves[leaf];
            var id = LeafId(leaf);
            if (ui.IsHovered(id))
                ui.SetCursor(MouseCursor.Text);
            var click = ui.RichText(shown.Runs, codeBackground: CodeChip, wrap: shown.Wrap, id: id, selected: Selection.SpanIn(leaf, shown.Text.Length));
            if (click.Link != null)
                Clicked = click;

            // After RichText: its links take the click first, which a second interaction would lose.
            var i = ui.Interact(id);
            if (i.Held)
                _held = true;
            if (!i.Pressed || PositionIn(leaf) is not { } at)
                return;
            _pressed = true;
            Selection.Press(Leaves, at, i.ClickCount, IsShiftHeld(ui));
            ui.Focus(_focusId, takesText: false);
            ui.RequestUpdate();
        }

        /// <summary>The code on a bordered panel, under its language when the fence names one and the
        /// button that copies it.</summary>
        private void Code(ShownBlock.Code code)
        {
            var theme = ui.Theme;
            using (ui.Column(new Box
            {
                Width = Sizing.Grow(),
                Padding = new Spacing(theme.PanelPadding, theme.Gap),
                Gap = theme.GapTight,
                Background = theme.Surface,
                BorderColor = theme.Border,
                BorderWidth = theme.BorderWidth,
                Radius = theme.PanelRadius,
            }))
            {
                CodeHeader(code);
                using (ui.Scroll(ui.GetId(HashCode.Combine(MarkdownScrollSalt, code.Leaf)), ScrollAxes.Horizontal, new Box { Gap = 0f, AlignY = Align.Start }))
                    Leaf(code.Leaf);
            }
        }

        private void CodeHeader(ShownBlock.Code code)
        {
            if (code.Language == null && look.CopyIcon == null)
                return;
            var theme = ui.Theme;
            using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Center }))
            {
                if (code.Language != null)
                    ui.Text(code.Language, color: theme.TextMuted, size: theme.FontSizeSmall);
                ui.Spacer();
                if (look.CopyIcon is { } copy)
                    CopyButton(code.Leaf, copy);
            }
        }

        /// <summary>The button that copies a code block's code, showing
        /// <see cref="MarkdownLook.CopiedIcon"/> for a moment after it did.</summary>
        private void CopyButton(int leaf, IconGlyph copy)
        {
            var copied = state.CopiedLeaf == leaf && ui.RealTime - state.CopiedAt < CopiedSeconds;
            var icon = copied ? look.CopiedIcon ?? copy : copy;
            if (!ui.IconButton($"copy-{leaf}", icon, copied ? CopiedCodeTooltip : CopyCodeTooltip))
                return;
            ui.ClipboardText = Leaves[leaf].Text;
            state.CopiedLeaf = leaf;
            state.CopiedAt = ui.RealTime;
            ui.RequestUpdateIn(CopiedSeconds);
        }

        /// <summary>Each item behind its marker, the markers of a list as wide as its widest, so the
        /// items line up.</summary>
        private void List(ShownBlock.List list)
        {
            var theme = ui.Theme;
            var markerWidth = MarkerWidth(list);
            using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight }))
            {
                foreach (var item in list.Items)
                {
                    using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.GapTight, AlignY = Align.Start }))
                    {
                        Marker(item.Marker, markerWidth);
                        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.GapTight }))
                            Blocks(item.Blocks);
                    }
                }
            }
        }

        private float MarkerWidth(ShownBlock.List list)
        {
            var width = ui.Theme.CheckboxSize;
            foreach (var item in list.Items)
            {
                if (item.Marker is ItemMarker.Label label)
                    width = MathF.Max(width, ui.MeasureText(label.Text));
            }
            return MathF.Ceiling(width);
        }

        private void Marker(ItemMarker marker, float width)
        {
            var theme = ui.Theme;
            switch (marker)
            {
                case ItemMarker.Label label:
                    ui.Text(label.Text, new Box { Width = width }, color: theme.TextMuted, align: Align.End);
                    break;
                case ItemMarker.Task task:
                    using (ui.Row(new Box { Width = width, AlignX = Align.End }))
                    using (ui.Box(new Box
                    {
                        Width = theme.CheckboxSize,
                        Height = theme.CheckboxSize,
                        BorderColor = theme.Border,
                        BorderWidth = theme.BorderWidth,
                        Radius = theme.RadiusSmall,
                    }))
                    {
                        if (task.Checked)
                            ui.PaintGlyph(UiGlyph.Checkmark, theme.Text);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"No way to draw a {marker.GetType().Name}.");
            }
        }

        /// <summary>The quoted blocks behind a bar as tall as they are.</summary>
        private void Quote(ShownBlock.Quote quote)
        {
            var theme = ui.Theme;
            using (ui.Row(new Box { Width = Sizing.Grow(), Gap = theme.Gap, AlignY = Align.Start }))
            {
                ui.Leaf(new Box { Width = QuoteBarWidth, Height = Sizing.Grow(), Background = theme.Border });
                using (ui.Column(new Box { Width = Sizing.Grow(), Gap = theme.Gap }))
                    Blocks(quote.Blocks);
            }
        }

        /// <summary>The table column by column, each as wide as its widest cell and each cell one row
        /// tall, so the rows line up across columns; scrolled sideways when wider than the view.</summary>
        private void Table(ShownBlock.Table table)
        {
            var theme = ui.Theme;
            var scrollId = ui.GetId(HashCode.Combine(MarkdownScrollSalt, table.Header.Count > 0 ? table.Header[0] : -1));
            using (ui.Scroll(scrollId, ScrollAxes.Horizontal, new Box { Gap = 0f, AlignY = Align.Start }))
            using (ui.Row(new Box { Gap = 0f, BorderColor = theme.Border, BorderWidth = theme.BorderWidth }))
            {
                for (var column = 0; column < table.Columns.Count; column++)
                {
                    var align = AlignOf(table.Columns[column]);
                    using (ui.Column(new Box { Gap = 0f }))
                    {
                        Cell(CellOf(table.Header, column), align, theme.Surface);
                        foreach (var row in table.Rows)
                            Cell(CellOf(row, column), align, 0u);
                    }
                }
            }
        }

        /// <summary>The leaf of the cell in <paramref name="column"/>, -1 for one the row leaves out.</summary>
        private static int CellOf(IReadOnlyList<int> cells, int column) => column < cells.Count ? cells[column] : -1;

        private void Cell(int leaf, Align align, uint background)
        {
            var theme = ui.Theme;
            using (ui.Row(new Box
            {
                Width = Sizing.Grow(),
                Height = theme.RowHeight,
                Padding = new Spacing(theme.FieldPaddingX, 0f),
                AlignX = align,
                AlignY = Align.Center,
                Background = background,
                Clip = true,
            }))
            {
                if (leaf >= 0 && Leaves[leaf].Text.Length > 0)
                    Leaf(leaf);
            }
        }

        private static Align AlignOf(ColumnAlignment alignment) => alignment switch
        {
            ColumnAlignment.None or ColumnAlignment.Left => Align.Start,
            ColumnAlignment.Center => Align.Center,
            ColumnAlignment.Right => Align.End,
            _ => throw new InvalidOperationException($"No alignment {alignment}."),
        };
    }
}
