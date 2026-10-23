using ZGF.Gui;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy;

/// <summary>What hovering a type or member in code shows: its declaration highlighted as code, then
/// its documentation, a heading over each kind of section after the summary.</summary>
internal static class CodeTooltipView
{
    public const float MaxWidth = 560f;
    private const string LabelSeparator = " – ";
    private const float CodeChipMix = 0.1f;

    public static void Render(Ui ui, Strings t, CodeTooltip tooltip)
    {
        var blocks = BlocksOf(ui, t, tooltip);
        using (ui.Column(new Box { Gap = ui.Theme.GapTight }))
        {
            foreach (var block in blocks.Items)
                ui.RichText(block, codeBackground: blocks.CodeBackground);
        }
    }

    /// <summary>The tooltip as rich text blocks, kept until another tooltip, palette, theme or
    /// language is shown.</summary>
    private sealed class Blocks
    {
        public CodeTooltip? Tooltip;
        public CodeViewPalette? Palette;
        public UiTheme? Theme;
        public Strings? Strings;
        public uint CodeBackground;
        public readonly List<IReadOnlyList<RichTextRun>> Items = new();

        public bool IsBuiltFor(CodeTooltip tooltip, CodeViewPalette palette, UiTheme theme, Strings strings)
        {
            var sameLook = ReferenceEquals(Palette, palette) && ReferenceEquals(Theme, theme);
            return ReferenceEquals(Tooltip, tooltip) && sameLook && ReferenceEquals(Strings, strings);
        }
    }

    private static Blocks BlocksOf(Ui ui, Strings t, CodeTooltip tooltip)
    {
        var blocks = ui.GetState<Blocks>(ui.GetId("code-tooltip"));
        var palette = ui.GetCodeViewPalette();
        if (blocks.IsBuiltFor(tooltip, palette, ui.Theme, t))
            return blocks;
        blocks.Tooltip = tooltip;
        blocks.Palette = palette;
        blocks.Theme = ui.Theme;
        blocks.Strings = t;
        blocks.CodeBackground = UiColor.Mix(ui.Theme.SurfaceRaised, ui.Theme.Text, CodeChipMix);
        blocks.Items.Clear();
        Build(blocks.Items, new TooltipStyles(ui, palette), t, tooltip);
        return blocks;
    }

    private static void Build(List<IReadOnlyList<RichTextRun>> items, TooltipStyles styles, Strings t, CodeTooltip tooltip)
    {
        foreach (var line in tooltip.Signature)
            items.Add(SignatureRuns(styles, line));
        DocSectionKind? previous = null;
        foreach (var section in tooltip.Sections)
        {
            var heading = HeadingOf(t, section.Kind);
            if (heading != null && section.Kind != previous)
                items.Add([new RichTextRun(heading, styles.Heading)]);
            previous = section.Kind;
            AddSection(items, styles, section);
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
    private static void AddSection(List<IReadOnlyList<RichTextRun>> items, TooltipStyles styles, DocSection section)
    {
        var label = LabelRuns(styles, section);
        if (section.Paragraphs.Count == 0)
        {
            if (label.Count > 0)
                items.Add(label);
            return;
        }
        for (var i = 0; i < section.Paragraphs.Count; i++)
        {
            var runs = new List<RichTextRun>();
            if (i == 0)
                runs.AddRange(label);
            AddParagraph(runs, styles, section.Paragraphs[i]);
            items.Add(runs);
        }
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

    /// <summary>The text styles of a tooltip in the current theme and code palette.</summary>
    private sealed class TooltipStyles(Ui ui, CodeViewPalette palette)
    {
        private readonly float _size = ui.Theme.FontSizeSmall;

        public TextStyle Plain { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSizeSmall);
        public TextStyle Bold { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSizeSmall, bold: true);
        public TextStyle Italic { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSizeSmall, family: TextFonts.Italic);
        public TextStyle Heading { get; } = ui.StyleText(ui.Theme.TextMuted, ui.Theme.FontSizeSmall, bold: true);
        public TextStyle CodeBlock { get; } = ui.StyleText(ui.Theme.Text, ui.Theme.FontSizeSmall, family: MonoFonts.Regular);

        public TextStyle Code(SpanKind kind) => ui.StyleText(palette[kind], _size, family: MonoFonts.Regular);

        /// <summary>A type or member named in the text, colored as code colors what it points at.</summary>
        public TextStyle Reference(CodeReference? reference)
        {
            var kind = SpanKindOf(reference);
            return ui.StyleText(palette[kind], _size);
        }

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
