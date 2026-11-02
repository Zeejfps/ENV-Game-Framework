using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.Metadata;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class TargetFrameworkTests
{
    [Theory]
    [InlineData(".NETCoreApp,Version=v10.0", "net10.0")]
    [InlineData(".NETCoreApp,Version=v3.1", "netcoreapp3.1")]
    [InlineData(".NETStandard,Version=v2.0", "netstandard2.0")]
    [InlineData(".NETFramework,Version=v4.8.1", "net481")]
    [InlineData(".NETFramework,Version=v4.7.2", "net472")]
    [InlineData("Silverlight,Version=v5.0", "Silverlight 5.0")]
    public void A_framework_id_reads_as_its_moniker(string id, string moniker)
    {
        Assert.Equal(moniker, Frameworks.Id(id).Moniker);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(",Version=v4.8")]
    [InlineData(".NETFramework,Version=vNext")]
    [InlineData(".NETFramework,Version=4.8")]
    public void Text_that_is_not_a_framework_id_does_not_read_as_one(string text)
    {
        Assert.Null(TargetFrameworkId.Parse(text));
    }

    [Fact]
    public void A_framework_id_with_a_profile_keeps_it()
    {
        var id = Frameworks.Id(".NETPortable,Version=v4.5,Profile=Profile7");

        Assert.Equal(".NETPortable", id.Name);
        Assert.Equal(".NETPortable, v4.5,Profile=Profile7", id.Readable);
    }

    [Theory]
    [InlineData(".NETCoreApp,Version=v10.0", true)]
    [InlineData(".NETStandard,Version=v2.0", true)]
    [InlineData(null, true)]
    [InlineData(".NETFramework,Version=v4.8", false)]
    [InlineData("Silverlight,Version=v5.0", false)]
    public void Only_an_assembly_for_dotnet_core_or_standard_or_no_known_framework_runs_on_core(string? id, bool runsOnCore)
    {
        Assert.Equal(runsOnCore, FrameworkIds.RunsOnCore(TargetFrameworkId.Parse(id)));
    }

    [Fact]
    public void An_assembly_for_dotnet_framework_resolves_its_references_for_dotnet_framework_not_the_running_runtime()
    {
        var list = new AssemblyList();
        var detected = list.OpenModule(Trees.TestAssemblyPath);
        var reference = AssemblyNameReference.Parse("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
        var onCore = detected.Resolver.FindFile(reference);
        list.TargetFrameworks.Pick(detected.FilePath, Frameworks.Net48);
        list.Clear();
        var picked = list.OpenModule(Trees.TestAssemblyPath);

        var onFramework = picked.Resolver.FindFile(reference);

        var framework = new UniversalAssemblyResolver(Trees.TestAssemblyPath, false, Frameworks.Net48.Text, null, PEStreamOptions.PrefetchEntireImage, MetadataReaderOptions.Default);
        Assert.Equal(RuntimeAssemblies.Directories[0], Path.GetDirectoryName(onCore!.Text), ignoreCase: true);
        Assert.Equal(framework.FindAssemblyFile(reference), onFramework?.Text);
    }

    [Fact]
    public void An_assembly_resolves_against_the_framework_picked_for_it_once_it_is_opened_again()
    {
        var list = new AssemblyList();
        var detected = list.OpenModule(Trees.TestAssemblyPath);

        list.TargetFrameworks.Pick(detected.FilePath, Frameworks.Net48);
        list.Clear();
        var picked = list.OpenModule(Trees.TestAssemblyPath);

        Assert.False(detected.IsTargetFrameworkPicked);
        Assert.True(picked.IsTargetFrameworkPicked);
        Assert.Equal(Frameworks.Net48, picked.TargetFramework);
        Assert.Equal("net48", picked.FrameworkMoniker);
        Assert.Equal(detected.TargetFramework, picked.DetectedTargetFramework);
    }

    [Fact]
    public void Closing_an_assembly_forgets_the_framework_picked_for_it()
    {
        var list = new AssemblyList();
        var assembly = list.Open(Trees.TestAssemblyPath);
        list.TargetFrameworks.Pick(assembly.FilePath, Frameworks.Net48);

        list.Close(assembly);

        Assert.Empty(list.TargetFrameworks.All);
    }
}
