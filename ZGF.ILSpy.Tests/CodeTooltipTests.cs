using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class CodeTooltipTests
{
    private static CodeTooltip TooltipOf(AssemblyList list, BrowseNode node) => list.Describe(node.Reference!)!;

    private static DocSection Section(CodeTooltip tooltip, DocSectionKind kind) => tooltip.Sections.Single(s => s.Kind == kind);

    private static string TextOf(DocParagraph paragraph) => string.Concat(paragraph.Runs.Select(r => r.Text));

    private static DocRun RunOf(DocParagraph paragraph, string text) => paragraph.Runs.Single(r => r.Text == text);

    /// <summary>The signature's text from each parameter's start to the next one's, the last to the end.</summary>
    private static List<string> ParameterTexts(CodeSignature signature)
    {
        var text = signature.Line.Text;
        var starts = signature.ParameterStarts;
        var parameters = new List<string>();
        for (var i = 0; i < starts.Count; i++)
        {
            var end = i + 1 < starts.Count ? starts[i + 1] : text.Length;
            parameters.Add(text.Substring(starts[i], end - starts[i]));
        }
        return parameters;
    }

    [Fact]
    public void The_signature_is_the_declaration_highlighted_as_code_with_its_names_linked()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var add = assembly.Calculator().Child("Add(T) : void");

        var signature = TooltipOf(list, add).Signature.Line;

        Assert.Equal("public void Calculator<T>.Add(T item)", signature.Text);
        Assert.Equal(SpanKind.Keyword, signature.SpanAt("public").Kind);
        Assert.Equal(SpanKind.Parameter, signature.SpanAt("item").Kind);
        Assert.Equal(add.Reference, signature.SpanAt("Add").Reference);
    }

    [Fact]
    public void The_signature_names_types_by_their_short_names_and_says_where_the_entity_is()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var mode = TooltipOf(list, assembly.Calculator().Child("Mode"));

        Assert.Equal("public enum Calculator<T>.Mode", mode.Signature.Line.Text);
        Assert.Equal("ZGF.ILSpy.Tests.Fixtures", mode.Namespace);
        Assert.Equal("ZGF.ILSpy.Tests", mode.Assembly);
    }

    [Fact]
    public void The_signature_marks_where_each_of_its_parameters_starts()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var tryGet = assembly.Calculator().Child("TryGet(int, out T?) : bool");

        var signature = TooltipOf(list, tryGet).Signature;
        var text = signature.Line.Text;

        Assert.Equal("public bool Calculator<T>.TryGet(int index, out T? item)", text);
        Assert.Equal(["int index, ", "out T? item)"], ParameterTexts(signature));
        Assert.Empty(TooltipOf(list, assembly.Calculator()).Signature.ParameterStarts);
    }

    [Fact]
    public void The_summary_and_each_parameter_come_from_the_xml_documentation_in_order()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var calculator = assembly.Calculator();

        var tooltip = TooltipOf(list, calculator.Child("Add(T) : void"));
        var summary = Assert.Single(Section(tooltip, DocSectionKind.Summary).Paragraphs);
        var item = Section(tooltip, DocSectionKind.Parameter);
        var changed = RunOf(summary, "Calculator<T>.Changed");

        Assert.Equal([DocSectionKind.Summary, DocSectionKind.Parameter], tooltip.Sections.Select(s => s.Kind));
        Assert.Equal("Adds an item and raises Calculator<T>.Changed.", TextOf(summary));
        Assert.Equal(DocStyle.Reference, changed.Style);
        Assert.Same(calculator.Child("Changed : EventHandler?"), list.FindNode(changed.Reference!));
        Assert.Equal("item", item.Label!.Value.Text);
        Assert.Equal("The item to add.", TextOf(Assert.Single(item.Paragraphs)));
    }

    [Fact]
    public void Remarks_keep_their_paragraphs_list_items_and_code_blocks()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var grow = assembly.Fixtures().Child("Circle", "Grow(double, int) : double");

        var remarks = Section(TooltipOf(list, grow), DocSectionKind.Remarks).Paragraphs;

        Assert.Equal(["Each step multiplies once:", "• Zero – keeps the radius.", "• More grows it.", "var grown = circle.Grow(2, 3);\nConsole.WriteLine(grown);"],
            remarks.Select(TextOf));
        Assert.Equal(DocStyle.Bold, RunOf(remarks[1], "Zero").Style);
        Assert.True(remarks[3].IsCode);
    }

    [Fact]
    public void Inline_code_keywords_and_links_keep_their_styles()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var grow = assembly.Fixtures().Child("Circle", "Grow(double, int) : double");

        var tooltip = TooltipOf(list, grow);
        var summary = Section(tooltip, DocSectionKind.Summary).Paragraphs[0];
        var returns = Section(tooltip, DocSectionKind.Returns).Paragraphs[0];

        Assert.Equal("Multiplies the radius by factor, times over.", TextOf(summary));
        Assert.Equal(DocStyle.Code, RunOf(summary, "factor").Style);
        Assert.Equal(DocStyle.Code, RunOf(summary, "times").Style);
        Assert.Equal("The grown radius, true to the example.", TextOf(returns));
        Assert.Equal(DocStyle.Code, RunOf(returns, "true").Style);
        Assert.Equal(DocStyle.Plain, RunOf(returns, "the example").Style);
    }

    [Fact]
    public void An_exception_is_labelled_by_its_type_and_says_when_it_is_thrown()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var grow = assembly.Fixtures().Child("Circle", "Grow(double, int) : double");

        var exception = Section(TooltipOf(list, grow), DocSectionKind.Exception);

        Assert.Equal("ArgumentOutOfRangeException", exception.Label!.Value.Text);
        Assert.NotNull(exception.Label.Value.Reference);
        Assert.Equal("times is negative.", TextOf(Assert.Single(exception.Paragraphs)));
    }

    [Fact]
    public void Inheritdoc_takes_the_documentation_of_the_member_it_implements()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var area = assembly.Fixtures().Child("Circle", "Area : double");

        var summary = Section(TooltipOf(list, area), DocSectionKind.Summary);

        Assert.Equal("The space the shape covers.", TextOf(Assert.Single(summary.Paragraphs)));
    }

    [Fact]
    public void An_undocumented_type_has_only_its_signature()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var tooltip = TooltipOf(list, assembly.Calculator());

        Assert.Equal("public class Calculator<T>", tooltip.Signature.Line.Text);
        Assert.Empty(tooltip.Sections);
    }

    [Fact]
    public void A_reference_into_another_assembly_is_described_without_listing_it()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Child("Add(T) : void").Decompile(DecompilationLanguage.CSharp);
        var call = document.Lines[document.LineContaining("_items.Add(item)")];

        var tooltip = list.Describe(call.SpanAt("Add").Reference!)!;

        Assert.StartsWith("public void List<T>.Add(T item)", tooltip.Signature.Line.Text);
        Assert.Equal("System.Collections.Generic", tooltip.Namespace);
        Assert.Equal("System.Private.CoreLib", tooltip.Assembly);
        Assert.NotEmpty(Section(tooltip, DocSectionKind.Summary).Paragraphs);
        Assert.Equal([assembly], list.Assemblies);
    }

    [Fact]
    public void Locals_and_assemblies_have_no_tooltip()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Fixtures().Child("Circle", "Grow(double, int) : double").Decompile(DecompilationLanguage.CSharp);
        var local = document.Lines[document.LineContaining("double grown = Radius;")].SpanAt("grown").Reference!;
        var references = assembly.Root.Child("References").Decompile(DecompilationLanguage.CSharp);
        var runtime = references.SpansOf().First(s => s.Span.Reference?.Kind == CodeReferenceKind.Assembly).Span.Reference!;

        Assert.Null(list.Describe(local));
        Assert.Null(list.Describe(runtime));
    }
}
