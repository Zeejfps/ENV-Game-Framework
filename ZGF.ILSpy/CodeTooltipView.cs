using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What hovering a type or member in code shows: its declaration highlighted as code in a
/// band of its own, then a rule and its documentation, a heading over each kind of section after
/// the summary. Space between sections is wider than within them, so each reads as a group.</summary>
internal static class CodeTooltipView
{
    public const float PaddingX = 12f;
    private const float MaxWidth = 560f;
    private const float SignaturePaddingY = 8f;
    private const float DocumentationPaddingY = 10f;
    private const float SectionGap = 11f;
    private const float HeadingGap = 4f;
    private const float EntryGap = 4f;
    private const float ParagraphGap = 6f;
    private const string LabelSeparator = " – ";
    private const float CodeChipMix = 0.06f;
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
        using (ui.Column(new Box { Padding = new Spacing(PaddingX, SignaturePaddingY), Gap = 0f }))
        {
            foreach (var line in layout.Signature)
                ui.RichText(line, codeBackground: layout.CodeBackground);
        }
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
            using (ui.Column(new Box { Gap = EntryGap }))
            {
                foreach (var entry in group.Entries)
                    RenderEntry(ui, layout, entry);
            }
        }
    }

    private static void RenderEntry(Ui ui, TooltipLayout layout, List<IReadOnlyList<RichTextRun>> paragraphs)
    {
        using (ui.Column(new Box { Gap = ParagraphGap }))
        {
            foreach (var paragraph in paragraphs)
                ui.RichText(paragraph, codeBackground: layout.CodeBackground);
        }
    }

    /// <summary>A group of the documentation: the summary, or every section of one kind under its
    /// heading, one entry per section.</summary>
    private sealed record TooltipGroup(DocSectionKind Kind, IReadOnlyList<RichTextRun>? Heading)
    {
        public List<List<IReadOnlyList<RichTextRun>>> Entries { get; } = new();
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
        public readonly List<IReadOnlyList<RichTextRun>> Signature = new();
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
        Build(layout, new TooltipStyles(ui, palette), t, tooltip);
        return layout;
    }

    private static void Build(TooltipLayout layout, TooltipStyles styles, Strings t, CodeTooltip tooltip)
    {
        layout.Signature.Clear();
        layout.Groups.Clear();
        foreach (var line in tooltip.Signature)
            layout.Signature.Add(SignatureRuns(styles, line));
        foreach (var section in tooltip.Sections)
        {
            var group = GroupFor(layout.Groups, styles, t, section.Kind);
            group.Entries.Add(EntryOf(styles, section));
        }
    }

    private static IReadOnlyList<RichTextRun> SignatureRuns(TooltipStyles styles, DocumentLine line)
    {
        var runs = new List<RichTextRun>();
        foreach (var span in line.Spans)
        {
            var text = line.Text.Substring(span.Start, span.Length);
            runs.Add(new RichTextRun(text, styles.Code(span.Kind)));
        }
        return runs;
    }

    /// <summary>The group a section of <paramref name="kind"/> joins: the last one when it is of the
    /// same kind, else a new one under the kind's heading.</summary>
    private static TooltipGroup GroupFor(List<TooltipGroup> groups, TooltipStyles styles, Strings t, DocSectionKind kind)
    {
        if (groups.Count > 0 && groups[^1].Kind == kind)
            return groups[^1];
        var heading = HeadingRuns(styles, t, kind);
        var group = new TooltipGroup(kind, heading);
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

    /// <summary>The section's paragraphs, the first led by its label: the parameter or exception it
    /// is about.</summary>
    private static List<IReadOnlyList<RichTextRun>> EntryOf(TooltipStyles styles, DocSection section)
    {
        var label = LabelRuns(styles, section);
        var entry = new List<IReadOnlyList<RichTextRun>>();
        if (section.Paragraphs.Count == 0)
        {
            if (label.Count > 0)
                entry.Add(label);
            return entry;
        }
        for (var i = 0; i < section.Paragraphs.Count; i++)
        {
            var runs = new List<RichTextRun>();
            if (i == 0)
                runs.AddRange(label);
            AddParagraph(runs, styles, section.Paragraphs[i]);
            entry.Add(runs);
        }
        return entry;
    }

    private static List<RichTextRun> LabelRuns(TooltipStyles styles, DocSection section)
    {
        if (section.Label == null)
            return [];
        var label = section.Label.Value;
        return [Run(styles, label), new RichTextRun(LabelSeparator, styles.Plain)];
    }

    private static void AddParagraph(List<RichTextRun> runs, TooltipStyles styles, DocParagraph paragraph)
    {
        if (paragraph.IsCode)
        {
            runs.Add(new RichTextRun(paragraph.Runs[0].Text, styles.CodeBlock));
            return;
        }
        foreach (var run in paragraph.Runs)
            runs.Add(Run(styles, run));
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
