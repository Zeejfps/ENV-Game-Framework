using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class ForwardedTypeTests
{
    private static string RuntimeFile(string name) => Path.Combine(RuntimeAssemblies.Directories[0], name);

    [Fact]
    public void A_facade_lists_the_types_it_forwards_in_their_namespaces()
    {
        var facade = new AssemblyList().Open(RuntimeFile("System.Runtime.dll"));

        var generic = facade.Root.Child("System.Collections.Generic");
        var list = generic.Child("IList<>");
        var dictionary = generic.Child("IDictionary<,>");
        var exception = generic.Child("KeyNotFoundException");

        Assert.True(generic.IsPublicApi);
        Assert.Equal(NodeKind.ForwardedType, list.Kind);
        Assert.Equal("System.Private.CoreLib", list.ForwardedTo);
        Assert.Equal("System.Collections.Generic.IList<>", list.FullName);
        Assert.Equal(NodeKind.ForwardedType, dictionary.Kind);
        Assert.Equal(NodeKind.ForwardedType, exception.Kind);
        Assert.False(list.HasChildren);
    }

    [Fact]
    public void A_namespace_sorts_its_forwarded_types_by_name_then_by_how_many_type_parameters_they_have()
    {
        var facade = new AssemblyList().Open(RuntimeFile("System.Runtime.dll"));

        var labels = facade.Root.Child("System").Children.Select(c => c.Text).ToList();

        var sorted = labels
            .OrderBy(l => l.Split('<')[0], NaturalStringComparer.Instance)
            .ThenBy(l => l.Length);
        Assert.Equal(sorted, labels);
        Assert.Equal(["Action", "Action<>", "Action<,>"], labels.Where(l => l.StartsWith("Action")).Take(3));
    }

    [Fact]
    public void A_forwarded_type_goes_to_its_definition_however_many_forwards_away()
    {
        var list = new AssemblyList();
        var netstandard = list.Open(RuntimeFile("netstandard.dll"));
        var forwarded = netstandard.Root.Child("System.Collections.Generic").Child("List<>");

        var definition = list.FindNode(forwarded.Target!);

        Assert.Equal("System.Collections", forwarded.ForwardedTo);
        Assert.Equal(NodeKind.Class, definition!.Kind);
        Assert.Equal("List<T>", definition.Text);
        Assert.Equal("System.Private.CoreLib", definition.Assembly.Name);
        Assert.Contains(definition.Assembly, list.Assemblies);
    }

    [Fact]
    public void A_forwarded_type_reads_as_where_it_is_forwarded_to_with_a_link_to_its_definition()
    {
        var facade = new AssemblyList().Open(RuntimeFile("System.Runtime.dll"));
        var forwarded = facade.Root.Child("System.Collections.Generic").Child("IList<>");

        var document = forwarded.Decompile(DecompilationLanguage.CSharp);
        var namespaceDocument = forwarded.Parent!.Decompile(DecompilationLanguage.CSharp);

        Assert.Contains("// Forwarded to System.Private.CoreLib", document.GetText());
        Assert.Equal(forwarded.Target, document.Lines[document.LineContaining("System.Collections.Generic.IList<>")].SpanAt("System.Collections.Generic.IList<>").Reference);
        Assert.Contains("IList<> // Forwarded to System.Private.CoreLib", namespaceDocument.GetText());
    }
}
