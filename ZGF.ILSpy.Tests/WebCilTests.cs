using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.Tests;

public sealed class WebCilTests
{
    /// <summary>The magic and version every WebAssembly module starts with, followed here by nothing.</summary>
    private static readonly byte[] WebAssemblyHeader = [0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00];

    [Fact]
    public void A_webcil_module_opens_as_an_assembly_and_decompiles()
    {
        var assembly = new AssemblyList().Open(Trees.WebCilSamplePath);

        var counter = assembly.Root.Child("SampleApp").Child("Counter").Decompile(DecompilationLanguage.CSharp);

        Assert.IsNotType<FailedAssembly>(assembly);
        Assert.IsNotType<LoadedPackage>(assembly);
        Assert.Equal("SampleApp", assembly.Name);
        Assert.False(counter.IsError);
        Assert.Contains("public void Increment()", counter.GetText());
    }

    [Fact]
    public void A_webcil_module_shows_its_metadata_without_pe_headers()
    {
        var assembly = new AssemblyList().Open(Trees.WebCilSamplePath);

        var metadata = assembly.Root.Child("Metadata").Children;

        Assert.DoesNotContain(metadata, static c => c.Is(MetadataGridRole.Header));
        Assert.Contains(metadata, static c => c.Subject is NodeSubject.MetadataTables);
    }

    [Fact]
    public void A_webcil_module_offers_no_pdb_to_generate()
    {
        var assembly = new AssemblyList().OpenModule(Trees.WebCilSamplePath);

        Assert.False(assembly.CanGeneratePdb);
    }

    [Fact]
    public void The_summary_of_a_webcil_module_leaves_out_the_architecture()
    {
        var assembly = new AssemblyList().Open(Trees.WebCilSamplePath);

        var summary = assembly.Root.Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.Contains(Trees.WebCilSamplePath, summary);
        Assert.DoesNotContain("Architecture:", summary);
    }

    [Fact]
    public void A_webassembly_module_that_holds_no_assembly_fails_to_load()
    {
        using var temp = new TempDir();
        var path = temp.Write("dotnet.native.wasm", WebAssemblyHeader);

        var failed = new AssemblyList().Open(path);

        Assert.IsType<FailedAssembly>(failed);
    }
}
