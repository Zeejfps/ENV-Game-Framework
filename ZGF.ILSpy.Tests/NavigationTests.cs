using System.Reflection.Metadata.Ecma335;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class NavigationTests
{
    private static MetadataTableInfo TableOf(BrowseNode node) =>
        Assert.IsType<NodeSubject.MetadataTable>(node.Subject).Value;

    [Theory]
    [InlineData("N:ZGF.ILSpy.Tests.Fixtures", "ZGF.ILSpy.Tests.Fixtures")]
    [InlineData("T:ZGF.ILSpy.Tests.Fixtures.Circle", "Circle")]
    [InlineData("M:ZGF.ILSpy.Tests.Fixtures.Circle.Grow(System.Double,System.Int32)", "Grow(double, int) : double")]
    [InlineData("P:ZGF.ILSpy.Tests.Fixtures.Circle.Radius", "Radius : double")]
    public void A_documentation_id_finds_its_namespace_type_or_member(string id, string text)
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        var node = list.FindNodeById(id, [assembly]);

        Assert.Equal(text, node!.Text);
    }

    [Theory]
    [InlineData("T:ZGF.ILSpy.Tests.Fixtures.Square")]
    [InlineData("N:Nowhere")]
    [InlineData("Circle")]
    [InlineData("M:ZGF.ILSpy.Tests.Fixtures.Circle.Grow(")]
    public void A_documentation_id_no_assembly_has_or_none_can_read_finds_nothing(string id)
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        Assert.Null(list.FindNodeById(id, [assembly]));
    }

    [Fact]
    public void A_documentation_id_of_a_referenced_type_is_not_found_in_the_assembly_that_only_uses_it()
    {
        var (list, assembly) = Trees.OpenTestAssembly();

        Assert.Null(list.FindNodeById("T:System.String", [assembly]));
    }

    [Fact]
    public void A_reference_into_the_framework_opens_the_defining_assembly()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var line = document.Lines[document.LineContaining("private readonly List<T> _items")];

        var node = list.FindNode(line.SpanAt("List").Reference!);

        Assert.NotNull(node);
        Assert.Equal("List<T>", node!.Text);
        Assert.Equal(TypeShape.Class, node.TypeShape);
        Assert.Equal("System.Private.CoreLib", node.Assembly.Name);
        Assert.Contains(node.Assembly, list.Assemblies);
        Assert.Same(node, node.Assembly.Root.Child("System.Collections.Generic").Child("List<T>"));
    }

    [Fact]
    public void A_member_reference_lands_on_the_member_node()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var call = document.Lines[document.LineContaining("_items.Add(item);")];

        var addOnList = list.FindNode(call.SpanAt("Add").Reference!);
        var field = list.FindNode(call.SpanAt("_items").Reference!);

        Assert.Equal("Add(T) : void", addOnList!.Text);
        Assert.Equal("List<T>", addOnList.Parent!.Text);
        Assert.Same(assembly.Calculator().Child("_items : List<T>"), field);
    }

    [Fact]
    public void A_reference_shows_in_metadata_as_the_row_of_its_definition_in_the_assembly_that_defines_it()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var call = document.Lines[document.LineContaining("_items.Add(item);")];

        var add = list.FindMetadataRow(call.SpanAt("Add").Reference!)!.Value;
        var field = list.FindMetadataRow(call.SpanAt("_items").Reference!)!.Value;
        var self = list.FindMetadataRow(CodeReference.ToAssembly(assembly.Name, assembly.FilePath))!.Value;

        var methods = Assert.IsType<MetadataGrid>(TableOf(add.Node).Grid);
        var fields = Assert.IsType<MetadataGrid>(TableOf(field.Node).Grid);
        Assert.Equal("System.Private.CoreLib", add.Node.Assembly.Name);
        Assert.Equal(TableIndex.MethodDef, TableOf(add.Node).ListedTable);
        Assert.Equal("Add", methods.Text(add.Row, methods.Column("Name")));
        Assert.Same(assembly, field.Node.Assembly);
        Assert.Equal("_items", fields.Text(field.Row, fields.Column("Name")));
        Assert.Equal(TableIndex.Assembly, TableOf(self.Node).ListedTable);
        Assert.Equal(0, self.Row);
    }

    [Fact]
    public void A_local_has_no_metadata_row()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Child("Add(T) : void").Decompile(DecompilationLanguage.CSharp);
        var item = document.SpansOf().First(s => s.Text == "item").Span.Reference!;

        Assert.Null(list.FindMetadataRow(item));
    }

    [Fact]
    public void IL_type_references_resolve_through_the_referencing_assembly()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.IL);
        var objectReference = document.SpansOf().First(s => s.Text == "System.Object" && s.Span.Reference is not null).Span.Reference!;

        var node = list.FindNode(objectReference);

        Assert.Equal("Object", node!.Text);
        Assert.Equal("System.Private.CoreLib", node.Assembly.Name);
    }

    [Fact]
    public void A_node_finds_its_definition_line_in_its_parents_document()
    {
        var assembly = Trees.TestAssembly;
        var calculator = assembly.Calculator();

        var csharp = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var il = Trees.CalculatorDocument(DecompilationLanguage.IL);

        Assert.Contains("public void Add(T item)", csharp.Lines[csharp.FindDefinitionLine(calculator.Child("Add(T) : void"))].Text);
        Assert.Contains("public int Count", csharp.Lines[csharp.FindDefinitionLine(calculator.Child("Count : int"))].Text);
        Assert.Contains("Fast", csharp.Lines[csharp.FindDefinitionLine(calculator.Child("Mode").Child("Fast : Mode"))].Text);
        Assert.Contains("public class Calculator<T>", csharp.Lines[csharp.FindDefinitionLine(calculator)].Text);
        Assert.Contains(".method", il.Lines[il.FindDefinitionLine(calculator.Child("Describe() : string"))].Text);
        Assert.Equal(-1, csharp.FindDefinitionLine(assembly.Fixtures().Child("Size")));
        Assert.Equal(-1, csharp.FindDefinitionLine(assembly.Fixtures()));
    }

    [Fact]
    public void References_to_the_same_entity_are_equal_across_documents()
    {
        var calculator = Trees.TestAssembly.Calculator();
        var typeDocument = Trees.CalculatorDocument(DecompilationLanguage.CSharp);
        var memberDocument = calculator.Child("Describe() : string").Decompile(DecompilationLanguage.CSharp);

        var count = calculator.Child("Count : int").Definition;
        var countInType = typeDocument.SpansOf().Where(s => s.Text == "Count").Select(s => s.Span.Reference).ToArray();
        var countInMember = memberDocument.SpansOf().First(s => s.Text == "Count").Span.Reference;

        Assert.Equal(2, countInType.Count(r => r == count));
        Assert.Equal(3, countInType.Count(r => r != count && r!.Kind == CodeReferenceKind.Property));
        Assert.Equal(count, countInMember);
    }

    [Fact]
    public void Locals_match_each_other_but_are_not_navigable()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Child("Add(T) : void").Decompile(DecompilationLanguage.CSharp);

        var items = document.SpansOf().Where(s => s.Text == "item").Select(s => s.Span).ToArray();

        Assert.Equal(2, items.Length);
        Assert.True(items[0].IsDefinition);
        Assert.Equal(items[0].Reference, items[1].Reference);
        Assert.False(items[0].Reference!.IsNavigable);
        Assert.Null(list.FindNode(items[0].Reference!));
    }

    [Fact]
    public void Hidden_compiler_generated_entities_fall_back_to_the_nearest_visible_ancestor()
    {
        var list = new AssemblyList();
        var coreLib = list.Open(Trees.CoreLibPath);
        var lambdaOwner = coreLib.Root.Child("System.Collections.Generic").Child("List<T>");
        var document = lambdaOwner.Child("Count : int").Decompile(DecompilationLanguage.IL);
        var getter = document.SpansOf().First(s => s.Span.IsDefinition && s.Span.Reference?.Kind == CodeReferenceKind.Property).Span.Reference!;

        var node = list.FindNode(getter);

        Assert.Same(lambdaOwner.Child("Count : int"), node);
    }

    [Fact]
    public void IL_uses_through_member_references_match_their_definitions()
    {
        var calculator = Trees.TestAssembly.Calculator();
        var document = Trees.CalculatorDocument(DecompilationLanguage.IL);
        var add = calculator.Child("Add(T) : void").Definition;
        var items = calculator.Child("_items : List<T>").Definition;

        var uses = document.SpansOf().Where(s => s.Span is { IsDefinition: false, Reference: not null }).Select(s => s.Span.Reference).ToArray();

        Assert.Contains(add, uses);
        Assert.Contains(items, uses);
        Assert.Contains(".method", document.Lines[document.FindDefinitionLine(add)].Text);
    }
}
