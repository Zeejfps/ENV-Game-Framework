using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class CodeTooltipTests
{
    private static CodeTooltip TooltipOf(AssemblyList list, BrowseNode node) => list.Describe(node.Definition)!;

    private static DocSection Section(CodeTooltip tooltip, DocSectionKind kind) => tooltip.Sections.Single(s => s.Kind == kind);

    private static string TextOf(DocBlock block) => TextOf(Assert.IsType<DocParagraph>(block).Runs);

    private static string TextOf(IReadOnlyList<DocRun> runs) => string.Concat(runs.Select(r => r.Text));

    private static DocRun RunOf(DocBlock block, string text) => Assert.IsType<DocParagraph>(block).Runs.Single(r => r.Text == text);

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
        Assert.Equal(add.Definition, signature.SpanAt("Add").Reference);
    }

    [Fact]
    public void The_signature_names_types_by_their_short_names_and_says_what_the_entity_is_in()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var mode = TooltipOf(list, assembly.Calculator().Child("Mode"));
        var calculator = TooltipOf(list, assembly.Calculator());
        var point = TooltipOf(list, assembly.Fixtures().Child("Point"));

        Assert.Equal("public enum Calculator<T>.Mode", mode.Signature.Line.Text);
        Assert.Equal(new CodeContainer("class", "ZGF.ILSpy.Tests.Fixtures.Calculator<T>", false), mode.Container);
        Assert.Equal(new CodeContainer("namespace", "ZGF.ILSpy.Tests.Fixtures", true), calculator.Container);
        Assert.Equal("namespace", point.Container!.Keyword);
    }

    [Fact]
    public void Each_type_parameter_constraint_is_a_where_clause_of_its_own()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var pick = assembly.Fixtures().Child("Circle", "Pick<TKey,TValue>(TKey, TValue) : TKey");

        var signature = TooltipOf(list, pick).Signature;
        var constraints = signature.Constraints.Select(c => c.Text.Trim());

        Assert.Equal("public TKey Circle.Pick<TKey, TValue>(TKey key, TValue value)", signature.Line.Text);
        Assert.Equal(["where TKey : class, IComparable<TKey>, new()", "where TValue : struct"], constraints);
        Assert.Empty(TooltipOf(list, assembly.Calculator()).Signature.Constraints);
    }

    [Fact]
    public void A_constrained_type_parameter_is_colored_as_a_type_in_the_tooltip_and_the_code()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var pick = assembly.Fixtures().Child("Circle", "Pick<TKey,TValue>(TKey, TValue) : TKey");

        var constraint = TooltipOf(list, pick).Signature.Constraints[1];
        var code = pick.Decompile(DecompilationLanguage.CSharp);
        var clause = code.Lines[code.LineContaining("where TValue : struct")];

        Assert.Equal(SpanKind.Type, constraint.SpanAt("TValue").Kind);
        Assert.Equal(SpanKind.Type, clause.SpanAt("TValue").Kind);
    }

    [Fact]
    public void A_member_of_a_record_or_struct_is_in_one_by_its_keyword()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var x = TooltipOf(list, assembly.Fixtures().Child("Point", "X : int"));
        var width = TooltipOf(list, assembly.Fixtures().Child("Size", "Width : int"));

        Assert.Equal("record", x.Container!.Keyword);
        Assert.Equal("struct", width.Container!.Keyword);
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
        var summary = Assert.Single(Section(tooltip, DocSectionKind.Summary).Blocks);
        var item = Section(tooltip, DocSectionKind.Parameter);
        var changed = RunOf(summary, "Calculator<T>.Changed");

        Assert.Equal([DocSectionKind.Summary, DocSectionKind.Parameter], tooltip.Sections.Select(s => s.Kind));
        Assert.Equal("Adds an item and raises Calculator<T>.Changed.", TextOf(summary));
        Assert.Equal(DocStyle.Reference, changed.Style);
        Assert.Same(calculator.Child("Changed : EventHandler?"), list.FindNode(changed.Reference!));
        Assert.Equal("item", item.Label!.Value.Text);
        Assert.Equal("The item to add.", TextOf(Assert.Single(item.Blocks)));
    }

    [Fact]
    public void Remarks_keep_their_paragraphs_list_items_and_code_blocks()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var grow = assembly.Fixtures().Child("Circle", "Grow(double, int) : double");

        var remarks = Section(TooltipOf(list, grow), DocSectionKind.Remarks).Blocks;
        var bullets = Assert.IsType<DocList>(remarks[1]);
        var code = Assert.IsType<DocCode>(remarks[2]);

        Assert.Equal(3, remarks.Count);
        Assert.Equal("Each step multiplies once:", TextOf(remarks[0]));
        Assert.Equal(DocListKind.Bullet, bullets.Kind);
        Assert.Equal(["Zero", ""], bullets.Items.Select(i => TextOf(i.Term)));
        Assert.Equal(["keeps the radius.", "More grows it."], bullets.Items.Select(i => TextOf(i.Description)));
        Assert.Equal("var grown = circle.Grow(2, 3);\nConsole.WriteLine(grown);", code.Text);
    }

    [Fact]
    public void A_table_list_keeps_its_header_and_a_numbered_list_its_kind()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var spread = assembly.Fixtures().Child("Circle", "Spread() : double");

        var remarks = Section(TooltipOf(list, spread), DocSectionKind.Remarks).Blocks;
        var table = Assert.IsType<DocList>(remarks[0]);
        var numbered = Assert.IsType<DocList>(remarks[1]);

        Assert.Equal(DocListKind.Table, table.Kind);
        Assert.Equal("Bound", TextOf(table.Header!.Term));
        Assert.Equal("Value", TextOf(table.Header.Description));
        Assert.Equal(["Low", "High"], table.Items.Select(i => TextOf(i.Term)));
        Assert.Equal(["The radius.", "Twice the radius."], table.Items.Select(i => TextOf(i.Description)));
        Assert.Equal(DocListKind.Number, numbered.Kind);
        Assert.Equal(["First.", "Second."], numbered.Items.Select(i => TextOf(i.Description)));
    }

    [Fact]
    public void Inline_code_keywords_and_links_keep_their_styles()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var grow = assembly.Fixtures().Child("Circle", "Grow(double, int) : double");

        var tooltip = TooltipOf(list, grow);
        var summary = Section(tooltip, DocSectionKind.Summary).Blocks[0];
        var returns = Section(tooltip, DocSectionKind.Returns).Blocks[0];

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
        Assert.Equal("times is negative.", TextOf(Assert.Single(exception.Blocks)));
    }

    [Fact]
    public void Inheritdoc_takes_the_documentation_of_the_member_it_implements()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var area = assembly.Fixtures().Child("Circle", "Area : double");

        var summary = Section(TooltipOf(list, area), DocSectionKind.Summary);

        Assert.Equal("The space the shape covers.", TextOf(Assert.Single(summary.Blocks)));
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
        Assert.Equal(new CodeContainer("class", "System.Collections.Generic.List<T>", false), tooltip.Container);
        Assert.NotEmpty(Section(tooltip, DocSectionKind.Summary).Blocks);
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
