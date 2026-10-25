using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class MicrosoftDocsTests
{
    private const string ApiReference = "https://learn.microsoft.com/dotnet/api/";

    [Fact]
    public void Namespaces_types_and_members_open_their_pages_of_the_api_reference()
    {
        var coreLib = new AssemblyList().Open(Trees.CoreLibPath);
        var generic = coreLib.Root.Child("System.Collections.Generic");
        var list = generic.Child("List<T>");
        var dictionary = generic.Child("Dictionary<TKey, TValue>");
        var constructors = list.Children.Where(c => c.Text == "List()").ToList();
        var constructor = constructors.Single(c => c.IsPublicApi);
        var staticConstructor = constructors.Single(c => !c.IsPublicApi);

        Assert.Equal(ApiReference + "system.collections.generic", MicrosoftDocs.FindPage(generic));
        Assert.Equal(ApiReference + "system.collections.generic.list-1", MicrosoftDocs.FindPage(list));
        Assert.Equal(ApiReference + "system.collections.generic.list-1.add", MicrosoftDocs.FindPage(list.Child("Add(T) : void")));
        Assert.Equal(ApiReference + "system.collections.generic.list-1.-ctor", MicrosoftDocs.FindPage(constructor));
        Assert.Null(MicrosoftDocs.FindPage(staticConstructor));
        Assert.Equal(ApiReference + "system.collections.generic.dictionary-2.enumerator", MicrosoftDocs.FindPage(dictionary.Child("Enumerator")));
    }

    [Fact]
    public void A_reference_in_code_finds_the_page_of_its_definition_but_not_of_a_private_member_or_a_local()
    {
        var (list, assembly) = Trees.OpenTestAssembly();
        var document = assembly.Calculator().Decompile(DecompilationLanguage.CSharp);
        var call = document.Lines[document.LineContaining("_items.Add(item);")];
        var add = assembly.Calculator().Child("Add(T) : void").Decompile(DecompilationLanguage.CSharp);
        var item = add.SpansOf().First(s => s.Text == "item").Span.Reference!;

        Assert.Equal(ApiReference + "system.collections.generic.list-1.add", list.FindApiReferencePage(call.SpanAt("Add").Reference!));
        Assert.Null(list.FindApiReferencePage(call.SpanAt("_items").Reference!));
        Assert.Null(list.FindApiReferencePage(item));
        Assert.DoesNotContain(list.Listed, a => a.Name == "System.Private.CoreLib");
    }

    [Fact]
    public void What_is_not_visible_outside_its_assembly_has_no_page()
    {
        var assembly = Trees.TestAssembly;
        var calculator = assembly.Calculator();

        Assert.Null(MicrosoftDocs.FindPage(calculator.Child("_items : List<T>")));
        Assert.Null(MicrosoftDocs.FindPage(assembly.Root));
        Assert.Null(MicrosoftDocs.FindPage(assembly.Root.Child("References")));
    }
}
