using System.Reflection.Metadata;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class LoadOptionsTests
{
    private static DecompilerOptions Off(DecompilerToggle toggle) => DecompilerOptions.Default.With(toggle, false);

    /// <summary>A list that reads with <paramref name="options"/>, having opened the test assembly.</summary>
    private static (AssemblyList List, LoadedAssembly Assembly) OpenWith(DecompilerOptions options)
    {
        var list = new AssemblyList();
        list.UseLoadOptions(options);
        return (list, list.Open(Trees.TestAssemblyPath));
    }

    private static BrowseNode RuntimeReference(LoadedAssembly assembly) => assembly.Root.Child("References", "System.Runtime");

    [Fact]
    public void The_options_that_apply_as_assemblies_are_read_are_offered_among_the_other_decompiler_options()
    {
        var groups = new[] { DecompilerToggle.AutoLoadAssemblyReferences, DecompilerToggle.ApplyWindowsRuntimeProjections }
            .Select(t => t.Group);

        Assert.All(groups, g => Assert.Equal(DecompilerToggleGroup.Other, g));
    }

    [Fact]
    public void Load_options_report_a_change_only_when_they_differ_from_those_in_use()
    {
        var list = new AssemblyList();
        var off = Off(DecompilerToggle.AutoLoadAssemblyReferences);

        var unchanged = list.UseLoadOptions(DecompilerOptions.Default);
        var changed = list.UseLoadOptions(off);
        var again = list.UseLoadOptions(off);

        Assert.False(unchanged);
        Assert.True(changed);
        Assert.False(again);
    }

    [Fact]
    public void Without_loading_references_a_reference_to_an_assembly_not_listed_resolves_to_none()
    {
        var (_, assembly) = OpenWith(Off(DecompilerToggle.AutoLoadAssemblyReferences));

        var children = RuntimeReference(assembly).Children;

        Assert.All(children, c => Assert.Equal(NodeKind.ReferencedTypes, c.Kind));
    }

    [Fact]
    public void Without_loading_references_a_reference_resolves_to_the_listed_assembly()
    {
        var (list, assembly) = OpenWith(Off(DecompilerToggle.AutoLoadAssemblyReferences));
        var runtimePath = RuntimeAssemblies.Find("System.Runtime")!;
        list.Open(runtimePath);

        var children = RuntimeReference(assembly).Children;

        Assert.Contains(children, c => c.Text == "System.Private.CoreLib");
    }

    [Fact]
    public void Loading_the_dependencies_loads_them_even_without_loading_references()
    {
        var (list, assembly) = OpenWith(Off(DecompilerToggle.AutoLoadAssemblyReferences));

        var listed = list.ListReferences(assembly);

        Assert.Contains(listed, a => a.Name == "System.Runtime");
    }

    [Fact]
    public void Metadata_is_read_with_Windows_Runtime_projections_only_while_they_are_on()
    {
        var (_, projected) = OpenWith(DecompilerOptions.Default);
        var (_, plain) = OpenWith(Off(DecompilerToggle.ApplyWindowsRuntimeProjections));

        Assert.Equal(MetadataReaderOptions.ApplyWindowsRuntimeProjections, projected.File.Metadata.Options);
        Assert.Equal(MetadataReaderOptions.None, plain.File.Metadata.Options);
    }
}
