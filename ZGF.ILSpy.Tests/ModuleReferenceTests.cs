using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class ModuleReferenceTests
{
    /// <summary>The References folder of the runtime's assembly <paramref name="name"/>.</summary>
    private static BrowseNode ReferencesOf(string name)
    {
        var path = RuntimeAssemblies.Find(name)!;
        var assembly = new AssemblyList().Open(path);
        return assembly.Root.Child("References");
    }

    /// <summary>The References folder of System.Private.CoreLib, which calls into native modules.</summary>
    private static BrowseNode CoreLibReferences() => ReferencesOf("System.Private.CoreLib");

    [Fact]
    public void The_references_list_the_modules_an_assembly_calls_into_sorted_by_name()
    {
        var modules = CoreLibReferences().Children
            .Where(c => c.Kind == NodeKind.ModuleReference)
            .Select(c => c.Text)
            .ToList();

        Assert.Contains("QCall", modules);
        Assert.Equal(modules.Order(NaturalStringComparer.Instance), modules);
    }

    [Fact]
    public void Module_references_come_after_the_assembly_references()
    {
        var kinds = ReferencesOf("System.IO.Compression").Children
            .Select(c => c.Kind)
            .ToList();

        Assert.Contains(NodeKind.AssemblyReference, kinds);
        Assert.Contains(NodeKind.ModuleReference, kinds);
        Assert.Equal(kinds.OrderBy(k => k == NodeKind.ModuleReference), kinds);
    }

    [Fact]
    public void A_module_reference_reads_as_its_name_and_a_native_library_when_the_assembly_lists_no_such_file()
    {
        var qcall = CoreLibReferences().Child("QCall");

        var text = Assert.IsType<DocumentContent>(qcall.Content()).Document.GetText();

        Assert.Contains("// QCall", text);
        Assert.Contains("A native library", text);
    }

    [Fact]
    public void The_references_folder_reads_as_its_assemblies_then_its_modules()
    {
        var text = Assert.IsType<DocumentContent>(CoreLibReferences().Content()).Document.GetText();

        Assert.Contains("// QCall", text);
    }
}
