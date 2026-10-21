using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void A_reference_into_the_framework_opens_the_defining_assembly()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var line = document.Lines[document.LineContaining("private readonly List<T> _items")];

        var node = list.FindNode(line.SpanAt("List").Reference!);

        Assert.NotNull(node);
        Assert.Equal("List<T>", node!.Text);
        Assert.Equal(NodeKind.Class, node.Kind);
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
        var (_, assembly) = Trees.OpenTestAssembly();
        var calculator = assembly.Calculator();

        var csharp = calculator.Decompile(DecompilationLanguage.CSharp);
        var il = calculator.Decompile(DecompilationLanguage.IL);

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
        var (_, assembly) = Trees.OpenTestAssembly();
        var calculator = assembly.Calculator();
        var typeDocument = calculator.Decompile(DecompilationLanguage.CSharp);
        var memberDocument = calculator.Child("Describe() : string").Decompile(DecompilationLanguage.CSharp);

        var count = calculator.Child("Count : int").Reference;
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
}
