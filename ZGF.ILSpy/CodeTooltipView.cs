using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What hovering a type or member in code shows: its declaration highlighted as code in a
/// band of its own, on one line when it fits and else with a parameter per line, over the type or
/// namespace it is in and its <c>where</c> clauses; then a rule and its documentation, a heading over each kind of section
/// after the summary. Space between sections is wider than within them, so each reads as a group.
/// Parameters line up in a column beside their descriptions, an exception's description sits
/// under it, list items hang from their markers, and code blocks are set in a box.</summary>
internal static class CodeTooltipView
{
    public const float PaddingX = 12f;
    private const float MaxWidth = 560f;
    private const float SignaturePaddingY = 8f;
    private const float ContextGap = 2f;
    private const float ParameterIndent = 16f;
    private const float ConstraintGap = 6f;
    private const char KindMark = '\u0001';
    private const char NameMark = '\u0002';
    private const float DocumentationPaddingY = 10f;
    private const float SectionGap = 11f;
    private const float HeadingGap = 4f;
    private const float EntryGap = 4f;
    private const float ParagraphGap = 6f;
    private const float ItemGap = 3f;
    private const float ColumnGap = 12f;
    private const float MaxLabelWidth = 140f;
    private const float MarkerWidth = 16f;
    private const float MarkerGap = 4f;
    private const float ExceptionIndent = 16f;
    private const float ExceptionGap = 2f;
    private const float CodeBlockPadding = 8f;
    private const float CodeBlockRadius = 4f;
    private const string Bullet = "•";
    private const string TermSeparator = " – ";
    private const float CodeChipMix = 0.06f;
    private const float CodeBlockMix = 0.3f;
    private const float HeadingMix = 0.2f;

    /// <summary>The popup the tooltip is shown in: unpadded, as the tooltip pads its own parts,
    /// and starting where the hovered word starts.</summary>
    public static readonly TooltipLook Look = new(MaxWidth, new Spacing(0f, 0f), Align.Start);

    public static void Render(Ui ui, Strings t, CodeTooltip tooltip)
    {
        var layout = LayoutOf(ui, t, tooltip);
        using (ui.Column(new Box { Gap = 0f }))
        {
            RenderSignature(ui, layout);
            if (layout.Groups.Count == 0)
                return;
            ui.Separator();
            RenderDocumentation(ui, layout);
        }
    }

    private static void RenderSignature(Ui ui, TooltipLayout layout)
    {
        using (ui.Column(new Box { Padding = new Spacing(PaddingX, SignaturePaddingY), Gap = ContextGap }))
        {
            RenderDeclaration(ui, layout);
            if (layout.Container.Count > 0)
                ui.RichText(layout.Container);
            RenderConstraints(ui, layout);
        }
    }

    private static void RenderConstraints(Ui ui, TooltipLayout layout)
    {
        if (layout.Constraints.Count == 0)
            return;
        using (ui.Column(new Box { Padding = new Spacing(0f, ConstraintGap, 0f, 0f), Gap = 0f }))
        {
            foreach (var constraint in layout.Constraints)
                ui.RichText(constraint);
        }
    }

    private static void RenderDeclaration(Ui ui, TooltipLayout layout)
    {
        if (FitsOnOneLine(ui, layout))
        {
            ui.RichText(layout.Declaration);
            return;
        }
        using (ui.Column(new Box { Gap = 0f }))
        {
            ui.RichText(layout.DeclarationHead);
            using (ui.Column(new Box { Padding = new Spacing(ParameterIndent, 0f, 0f, 0f), Gap = 0f }))
            {
                foreach (var parameter in layout.Parameters)
                    ui.RichText(parameter);
            }
        }
    }

    /// <summary>Whether the declaration stays on one line: when it has no parameters to break
    /// at, or fits the tooltip's width.</summary>
    private static bool FitsOnOneLine(Ui ui, TooltipLayout layout)
    {
        if (layout.Parameters.Count == 0)
            return true;
        var room = MaxWidth - 2 * PaddingX;
        return ui.MeasureRuns(layout.Declaration) <= room;
    }

    private static void RenderDocumentation(Ui ui, TooltipLayout layout)
    {
        using (ui.Column(new Box { Padding = new Spacing(PaddingX, DocumentationPaddingY), Gap = SectionGap }))
        {
            foreach (var group in layout.Groups)
                RenderGroup(ui, layout, group);
        }
    }

    private static void RenderGroup(Ui ui, TooltipLayout layout, TooltipGroup group)
    {
        using (ui.Column(new Box { Gap = HeadingGap }))
        {
            if (group.Heading != null)
                ui.RichText(group.Heading);
            var labelWidth = LabelWidth(ui, group.Entries);
            using (ui.Column(new Box { Gap = EntryGap }))
            {
                foreach (var entry in group.Entries)
                    RenderEntry(ui, layout, group.Shape, entry, labelWidth);
            }
        }
    }

    /// <summary>The width of a group's label column: its widest label and the gap after it, at
    /// most <see cref="MaxLabelWidth"/>.</summary>
    private static float LabelWidth(Ui ui, List<TooltipEntry> entries)
    {
        var widest = 0f;
        foreach (var entry in entries)
        {
            if (entry.Label != null)
                widest = MathF.Max(widest, ui.MeasureRuns(entry.Label));
        }
        return MathF.Min(widest, MaxLabelWidth) + ColumnGap;
    }

    private static void RenderEntry(Ui ui, TooltipLayout layout, EntryShape shape, TooltipEntry entry, float labelWidth)
    {
        switch (shape)
        {
            case EntryShape.Column when entry.Label != null:
                RenderColumnEntry(ui, layout, entry, labelWidth);
                break;
            case EntryShape.Stacked when entry.Label != null:
                RenderStackedEntry(ui, layout, entry);
                break;
            default:
                RenderBlocks(ui, layout, entry.Blocks);
                break;
        }
    }

    /// <summary>The label in its column, its blocks beside it.</summary>
    private static void RenderColumnEntry(Ui ui, TooltipLayout layout, TooltipEntry entry, float labelWidth)
    {
        using (ui.Row(new Box { Gap = 0f }))
        {
            using (ui.Column(new Box { Width = labelWidth }))
                ui.RichText(entry.Label!);
            using (ui.Column(new Box { Width = Sizing.Grow() }))
                RenderBlocks(ui, layout, entry.Blocks);
        }
    }

    /// <summary>The label on a line of its own, its blocks indented under it.</summary>
    private static void RenderStackedEntry(Ui ui, TooltipLayout layout, TooltipEntry entry)
    {
        using (ui.Column(new Box { Gap = ExceptionGap }))
        {
            ui.RichText(entry.Label!);
            using (ui.Column(new Box { Padding = new Spacing(ExceptionIndent, 0f, 0f, 0f) }))
                RenderBlocks(ui, layout, entry.Blocks);
        }
    }

    private static void RenderBlocks(Ui ui, TooltipLayout layout, IReadOnlyList<TooltipBlock> blocks)
    {
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = ParagraphGap }))
        {
            foreach (var block in blocks)
                RenderBlock(ui, layout, block);
        }
    }

    private static void RenderBlock(Ui ui, TooltipLayout layout, TooltipBlock block)
    {
        switch (block)
        {
            case TextBlock text:
                ui.RichText(text.Runs, codeBackground: layout.CodeBackground);
                break;
            case CodeBlock code:
                RenderCode(ui, layout, code);
                break;
            case ListBlock list:
                RenderList(ui, layout, list);
                break;
        }
    }

    private static void RenderCode(Ui ui, TooltipLayout layout, CodeBlock code)
    {
        var box = new Box
        {
            Width = Sizing.Grow(),
            Padding = new Spacing(CodeBlockPadding),
            Radius = CodeBlockRadius,
            Background = layout.CodeBlockBackground,
        };
        using (ui.Column(box))
            ui.RichText(code.Runs);
    }

    /// <summary>Each row's lead — a marker, or a table's term — in a column of its own, so wrapped
    /// lines hang under the text rather than the lead.</summary>
    private static void RenderList(Ui ui, TooltipLayout layout, ListBlock list)
    {
        var leadWidth = LeadWidth(ui, list);
        using (ui.Column(new Box { Width = Sizing.Grow(), Gap = ItemGap }))
        {
            foreach (var row in list.Rows)
                RenderListRow(ui, layout, row, leadWidth);
        }
    }

    /// <summary>A list's lead column: a table's widest term and the column gap, at most
    /// <see cref="MaxLabelWidth"/>; a marker's width, and more for numbers that need it.</summary>
    private static float LeadWidth(Ui ui, ListBlock list)
    {
        var widest = 0f;
        foreach (var row in list.Rows)
            widest = MathF.Max(widest, ui.MeasureRuns(row.Lead));
        if (list.IsTable)
            return MathF.Min(widest, MaxLabelWidth) + ColumnGap;
        return MathF.Max(MarkerWidth, widest + MarkerGap);
    }

    private static void RenderListRow(Ui ui, TooltipLayout layout, ListRow row, float leadWidth)
    {
        using (ui.Row(new Box { Gap = 0f }))
        {
            using (ui.Column(new Box { Width = leadWidth }))
                ui.RichText(row.Lead);
            using (ui.Column(new Box { Width = Sizing.Grow() }))
                ui.RichText(row.Content, codeBackground: layout.CodeBackground);
        }
    }

    /// <summary>How a group sets out its entries: parameters with their names in a column,
    /// exceptions with their descriptions under them, anything else as its blocks.</summary>
    private enum EntryShape
    {
        Blocks,
        Column,
        Stacked,
    }

    private abstract record TooltipBlock;

    private sealed record TextBlock(IReadOnlyList<RichTextRun> Runs) : TooltipBlock;

    private sealed record CodeBlock(IReadOnlyList<RichTextRun> Runs) : TooltipBlock;

    /// <summary>A list's rows, each a lead and its content: a marker and the item, or a table's
    /// term and its description.</summary>
    private sealed record ListBlock(bool IsTable, IReadOnlyList<ListRow> Rows) : TooltipBlock;

    private sealed record ListRow(IReadOnlyList<RichTextRun> Lead, IReadOnlyList<RichTextRun> Content);

    /// <summary>A section's label, the parameter or exception it is about, and its blocks.</summary>
    private sealed record TooltipEntry(IReadOnlyList<RichTextRun>? Label, IReadOnlyList<TooltipBlock> Blocks);

    /// <summary>A group of the documentation: the summary, or every section of one kind under its
    /// heading, one entry per section.</summary>
    private sealed record TooltipGroup(DocSectionKind Kind, IReadOnlyList<RichTextRun>? Heading, EntryShape Shape)
    {
        public List<TooltipEntry> Entries { get; } = new();
    }

    /// <summary>The tooltip as rich text, kept until another tooltip, palette, theme or language is
    /// shown.</summary>
    private sealed class TooltipLayout
    {
        public CodeTooltip? Tooltip;
        public CodeViewPalette? Palette;
        public UiTheme? Theme;
        public Strings? Strings;
        public uint CodeBackground;
        public uint CodeBlockBackground;
        public IReadOnlyList<RichTextRun> Declaration = [];
        public IReadOnlyList<RichTextRun> DeclarationHead = [];
        public readonly List<IReadOnlyList<RichTextRun>> Parameters = new();
        public IReadOnlyList<RichTextRun> Container = [];
        public readonly List<IReadOnlyList<RichTextRun>> Constraints = new();
        public readonly List<TooltipGroup> Groups = new();

        public bool IsBuiltFor(CodeTooltip tooltip, CodeViewPalette palette, UiTheme theme, Strings strings)
        {
            var sameLook = ReferenceEquals(Palette, palette) && ReferenceEquals(Theme, theme);
            return ReferenceEquals(Tooltip, tooltip) && sameLook && ReferenceEquals(Strings, strings);
        }
    }

    private static TooltipLayout LayoutOf(Ui ui, Strings t, CodeTooltip tooltip)
    {
        var layout = ui.GetState<TooltipLayout>(ui.GetId("code-tooltip"));
        var palette = ui.GetCodeViewPalette();
        if (layout.IsBuiltFor(tooltip, palette, ui.Theme, t))
            return layout;
        layout.Tooltip = tooltip;
        layout.Palette = palette;
        layout.Theme = ui.Theme;
        layout.Strings = t;
        layout.CodeBackground = UiColor.Mix(ui.Theme.SurfaceRaised, ui.Theme.Text, CodeChipMix);
        layout.CodeBlockBackground = UiColor.Mix(ui.Theme.SurfaceRaised, ui.Theme.EditorBackground, CodeBlockMix);
        Build(layout, new TooltipStyles(ui, palette), t, tooltip);
        return layout;
    }

    private static void Build(TooltipLayout layout, TooltipStyles styles, Strings t, CodeTooltip tooltip)
    {
        BuildDeclaration(layout, styles, tooltip.Signature);
        layout.Container = ContainerRuns(styles, t, tooltip.Container);
        BuildConstraints(layout, styles, tooltip.Signature);
        layout.Groups.Clear();
        foreach (var section in tooltip.Sections)
        {
            var group = GroupFor(layout.Groups, styles, t, section.Kind);
            group.Entries.Add(EntryOf(styles, section));
        }
    }

    /// <summary>The declaration on one line, and cut where each parameter starts: the head up to
    /// the first, then each parameter with its comma, the last with what follows it.</summary>
    private static void BuildDeclaration(TooltipLayout layout, TooltipStyles styles, CodeSignature signature)
    {
        var line = signature.Line;
        var starts = signature.ParameterStarts;
        layout.Declaration = CodeRuns(styles, line, 0, line.Text.Length);
        layout.Parameters.Clear();
        if (starts.Count == 0)
            return;
        layout.DeclarationHead = CodeRuns(styles, line, 0, starts[0]);
        for (var i = 0; i < starts.Count; i++)
        {
            var end = i + 1 < starts.Count ? starts[i + 1] : line.Text.Length;
            layout.Parameters.Add(CodeRuns(styles, line, starts[i], end));
        }
    }

    private static void BuildConstraints(TooltipLayout layout, TooltipStyles styles, CodeSignature signature)
    {
        layout.Constraints.Clear();
        foreach (var line in signature.Constraints)
        {
            var start = LeadingSpaces(line.Text);
            layout.Constraints.Add(CodeRuns(styles, line, start, line.Text.Length));
        }
    }

    private static int LeadingSpaces(string text)
    {
        var start = 0;
        while (start < text.Length && text[start] == ' ')
            start++;
        return start;
    }

    /// <summary>The line's text from <paramref name="from"/> to <paramref name="to"/> as runs in
    /// its spans' colors, without the spaces it ends with.</summary>
    private static IReadOnlyList<RichTextRun> CodeRuns(TooltipStyles styles, DocumentLine line, int from, int to)
    {
        var end = TrimmedEnd(line.Text, from, to);
        var runs = new List<RichTextRun>();
        foreach (var span in line.Spans)
        {
            var start = Math.Max(span.Start, from);
            var stop = Math.Min(span.End, end);
            if (stop <= start)
                continue;
            var text = line.Text.Substring(start, stop - start);
            runs.Add(new RichTextRun(text, styles.Code(span.Kind)));
        }
        return runs;
    }

    private static int TrimmedEnd(string text, int from, int to)
    {
        var end = to;
        while (end > from && text[end - 1] == ' ')
            end--;
        return end;
    }

    /// <summary>"in class Namespace.Type" or "in namespace Namespace", worded as the language words
    /// it, the keyword and name colored as code; nothing for the global namespace.</summary>
    private static IReadOnlyList<RichTextRun> ContainerRuns(TooltipStyles styles, Strings t, CodeContainer? container)
    {
        if (container == null)
            return [];
        var pattern = t.CodeTooltipContainer(KindMark, NameMark);
        var nameKind = container.IsNamespace ? SpanKind.Namespace : SpanKind.Type;
        var runs = new List<RichTextRun>();
        var start = 0;
        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] != KindMark && pattern[i] != NameMark)
                continue;
            AddContainerText(runs, styles, pattern.Substring(start, i - start));
            if (pattern[i] == KindMark)
                runs.Add(new RichTextRun(container.Keyword, styles.Code(SpanKind.Keyword)));
            else
                runs.Add(new RichTextRun(container.Name, styles.Code(nameKind)));
            start = i + 1;
        }
        AddContainerText(runs, styles, pattern.Substring(start));
        return runs;
    }

    private static void AddContainerText(List<RichTextRun> runs, TooltipStyles styles, string text)
    {
        if (text.Length > 0)
            runs.Add(new RichTextRun(text, styles.Container));
    }

    /// <summary>The group a section of <paramref name="kind"/> joins: the last one when it is of the
    /// same kind, else a new one under the kind's heading.</summary>
    private static TooltipGroup GroupFor(List<TooltipGroup> groups, TooltipStyles styles, Strings t, DocSectionKind kind)
    {
        if (groups.Count > 0 && groups[^1].Kind == kind)
            return groups[^1];
        var heading = HeadingRuns(styles, t, kind);
        var group = new TooltipGroup(kind, heading, ShapeOf(kind));
        groups.Add(group);
        return group;
    }

    private static IReadOnlyList<RichTextRun>? HeadingRuns(TooltipStyles styles, Strings t, DocSectionKind kind)
    {
        var heading = HeadingOf(t, kind);
        if (heading == null)
            return null;
        return [new RichTextRun(heading, styles.Heading)];
    }

    private static string? HeadingOf(Strings t, DocSectionKind kind) =>
        kind switch
        {
            DocSectionKind.TypeParameter => t.CodeTooltipTypeParameters,
            DocSectionKind.Parameter => t.CodeTooltipParameters,
            DocSectionKind.Returns => t.CodeTooltipReturns,
            DocSectionKind.Value => t.CodeTooltipValue,
            DocSectionKind.Exception => t.CodeTooltipExceptions,
            DocSectionKind.Remarks => t.CodeTooltipRemarks,
            DocSectionKind.Example => t.CodeTooltipExample,
            _ => null,
        };

    private static EntryShape ShapeOf(DocSectionKind kind) =>
        kind switch
        {
            DocSectionKind.TypeParameter => EntryShape.Column,
            DocSectionKind.Parameter => EntryShape.Column,
            DocSectionKind.Exception => EntryShape.Stacked,
            _ => EntryShape.Blocks,
        };

    private static TooltipEntry EntryOf(TooltipStyles styles, DocSection section)
    {
        var label = LabelRuns(styles, section);
        var blocks = new List<TooltipBlock>();
        foreach (var block in section.Blocks)
            blocks.Add(BlockOf(styles, block));
        return new TooltipEntry(label, blocks);
    }

    /// <summary>A parameter or type parameter by its name in code, an exception by its type.</summary>
    private static IReadOnlyList<RichTextRun>? LabelRuns(TooltipStyles styles, DocSection section)
    {
        if (section.Label == null)
            return null;
        var label = section.Label.Value;
        return section.Kind switch
        {
            DocSectionKind.TypeParameter => [new RichTextRun(label.Text, styles.Code(SpanKind.Type))],
            DocSectionKind.Parameter => [new RichTextRun(label.Text, styles.Code(SpanKind.Parameter))],
            _ => [Run(styles, label)],
        };
    }

    private static TooltipBlock BlockOf(TooltipStyles styles, DocBlock block) =>
        block switch
        {
            DocCode code => new CodeBlock([new RichTextRun(code.Text, styles.CodeBlock)]),
            DocList list => ListOf(styles, list),
            DocParagraph paragraph => new TextBlock(Runs(styles, paragraph.Runs)),
            _ => throw new ArgumentOutOfRangeException(nameof(block)),
        };

    private static ListBlock ListOf(TooltipStyles styles, DocList list)
    {
        var isTable = list.Kind == DocListKind.Table;
        var rows = new List<ListRow>();
        if (list.Header != null)
            rows.Add(HeaderRow(styles, list.Header, isTable));
        for (var i = 0; i < list.Items.Count; i++)
            rows.Add(ItemRow(styles, list, i));
        return new ListBlock(isTable, rows);
    }

    /// <summary>A table's heading row, its term over the terms and description over the
    /// descriptions; another list's leading item, without a marker.</summary>
    private static ListRow HeaderRow(TooltipStyles styles, DocListItem header, bool isTable)
    {
        if (!isTable)
            return new ListRow([], TermAndDescription(styles, header));
        var term = TextOf(header.Term);
        var description = TextOf(header.Description);
        return new ListRow([new RichTextRun(term, styles.Heading)], [new RichTextRun(description, styles.Heading)]);
    }

    private static ListRow ItemRow(TooltipStyles styles, DocList list, int index)
    {
        var item = list.Items[index];
        if (list.Kind == DocListKind.Table)
            return new ListRow(Runs(styles, item.Term), Runs(styles, item.Description));
        var marker = list.Kind == DocListKind.Number ? $"{index + 1}." : Bullet;
        return new ListRow([new RichTextRun(marker, styles.Plain)], TermAndDescription(styles, item));
    }

    /// <summary>The item's term in bold and its description after a dash, or the description alone.</summary>
    private static IReadOnlyList<RichTextRun> TermAndDescription(TooltipStyles styles, DocListItem item)
    {
        var description = Runs(styles, item.Description);
        if (item.Term.Count == 0)
            return description;
        var runs = new List<RichTextRun> { new(TextOf(item.Term), styles.Bold), new(TermSeparator, styles.Plain) };
        runs.AddRange(description);
        return runs;
    }

    private static string TextOf(IReadOnlyList<DocRun> runs) => string.Concat(runs.Select(static r => r.Text));

    private static List<RichTextRun> Runs(TooltipStyles styles, IReadOnlyList<DocRun> runs)
    {
        var converted = new List<RichTextRun>();
        foreach (var run in runs)
            converted.Add(Run(styles, run));
        return converted;
    }

    private static RichTextRun Run(TooltipStyles styles, DocRun run) =>
        run.Style switch
        {
            DocStyle.Bold => new RichTextRun(run.Text, styles.Bold),
            DocStyle.Italic => new RichTextRun(run.Text, styles.Italic),
            DocStyle.Code => new RichTextRun(run.Text, styles.CodeBlock, IsCode: true),
            DocStyle.Reference => new RichTextRun(run.Text, styles.Reference(run.Reference)),
            _ => new RichTextRun(run.Text, styles.Plain),
        };

    /// <summary>The text styles of a tooltip in the current theme and code palette: prose at the
    /// theme's text size, headings smaller and muted.</summary>
    private sealed class TooltipStyles(Ui ui, CodeViewPalette palette)
    {
        private readonly float _size = ui.Theme.FontSize;

        public TextStyle Plain { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize);
        public TextStyle Bold { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, bold: true);
        public TextStyle Italic { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, family: TextFonts.Italic);
        public TextStyle Heading { get; } = ui.StyleText(HeadingColor(ui.Theme), ui.Theme.FontSizeSmall, bold: true);
        public TextStyle Container { get; } = ui.StyleText(ui.Theme.TextMuted, ui.Theme.FontSize, family: MonoFonts.Regular);
        public TextStyle CodeBlock { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSize, family: MonoFonts.Regular);

        public TextStyle Code(SpanKind kind) => ui.StyleText(palette[kind], _size, family: MonoFonts.Regular);

        /// <summary>A type or member named in the text, colored as code colors what it points at.</summary>
        public TextStyle Reference(CodeReference? reference)
        {
            var kind = SpanKindOf(reference);
            return ui.StyleText(palette[kind], _size);
        }

        private static uint HeadingColor(UiTheme theme) => UiColor.Mix(theme.TextMuted, theme.Text, HeadingMix);

        private static SpanKind SpanKindOf(CodeReference? reference) =>
            reference?.Kind switch
            {
                CodeReferenceKind.Type => SpanKind.Type,
                CodeReferenceKind.Method => SpanKind.Method,
                CodeReferenceKind.Field => SpanKind.Field,
                CodeReferenceKind.Property => SpanKind.Property,
                CodeReferenceKind.Event => SpanKind.Event,
                _ => SpanKind.Type,
            };
    }
}
