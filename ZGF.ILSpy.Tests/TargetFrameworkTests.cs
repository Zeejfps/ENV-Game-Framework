using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class TargetFrameworkTests
{
    private const string Net48 = ".NETFramework,Version=v4.8";

    [Theory]
    [InlineData(".NETCoreApp,Version=v10.0", "net10.0")]
    [InlineData(".NETCoreApp,Version=v3.1", "netcoreapp3.1")]
    [InlineData(".NETStandard,Version=v2.0", "netstandard2.0")]
    [InlineData(".NETFramework,Version=v4.8.1", "net481")]
    [InlineData(".NETFramework,Version=v4.7.2", "net472")]
    [InlineData("Silverlight,Version=v5.0", "Silverlight 5.0")]
    [InlineData("Unknown", "Unknown")]
    public void A_framework_id_reads_as_its_moniker(string id, string moniker)
    {
        Assert.Equal(moniker, FrameworkIds.MonikerOf(id));
    }

    [Fact]
    public void An_assembly_resolves_against_the_framework_picked_for_it_once_it_is_opened_again()
    {
        var list = new AssemblyList();
        var detected = list.Open(Trees.TestAssemblyPath);

        list.TargetFrameworks.Pick(detected.FilePath, Net48);
        list.Clear();
        var picked = list.Open(Trees.TestAssemblyPath);

        Assert.False(detected.IsTargetFrameworkPicked);
        Assert.True(picked.IsTargetFrameworkPicked);
        Assert.Equal(Net48, picked.TargetFramework);
        Assert.Equal("net48", picked.FrameworkMoniker);
        Assert.Equal(detected.TargetFramework, picked.DetectedTargetFramework);
    }

    [Fact]
    public void Closing_an_assembly_forgets_the_framework_picked_for_it()
    {
        var list = new AssemblyList();
        var assembly = list.Open(Trees.TestAssemblyPath);
        list.TargetFrameworks.Pick(assembly.FilePath, Net48);

        list.Close(assembly);

        Assert.Empty(list.TargetFrameworks.All);
    }
}
