using System.Reflection.Metadata;
using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly TempDir _temp = new();

    public ExportTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void A_type_saves_as_the_code_it_shows()
    {
        var calculator = Trees.TestAssembly.Calculator();
        var path = _temp.PathOf("Calculator.cs");

        calculator.SaveCode(DecompilationLanguage.CSharp, DecompilerOptions.Default, path);

        var shown = Trees.CalculatorDocument(DecompilationLanguage.CSharp).GetText();
        Assert.Equal(shown, File.ReadAllText(path));
    }

    [Fact]
    public void A_namespace_saves_all_its_types()
    {
        var fixtures = Trees.TestAssembly.Fixtures();
        var path = _temp.PathOf("Fixtures.cs");

        fixtures.SaveCode(DecompilationLanguage.CSharp, DecompilerOptions.Default, path);

        var code = File.ReadAllText(path);
        Assert.Contains("public class Calculator<T>", code);
        Assert.Contains("class Circle", code);
    }

    [Fact]
    public void An_assembly_saves_its_whole_module_in_IL()
    {
        var path = _temp.PathOf("Tests.il");

        Trees.TestAssembly.Root.SaveCode(DecompilationLanguage.IL, DecompilerOptions.Default, path);

        var code = File.ReadAllText(path);
        Assert.Contains(".assembly ZGF.ILSpy.Tests", code);
        Assert.Contains("ZGF.ILSpy.Tests.Fixtures.Calculator`1", code);
    }

    [Fact]
    public void Code_files_are_offered_under_the_node_name_with_the_language_extension()
    {
        var calculator = Trees.TestAssembly.Calculator();

        Assert.Equal("Calculator.cs", calculator.SuggestCodeFileName(DecompilationLanguage.CSharp));
        Assert.Equal("Calculator.il", calculator.SuggestCodeFileName(DecompilationLanguage.IL));
        Assert.Equal("ZGF.ILSpy.Tests.cs", Trees.TestAssembly.Root.SuggestCodeFileName(DecompilationLanguage.CSharp));
        Assert.Equal("ZGF.ILSpy.Tests.csproj", Trees.TestAssembly.ProjectFileName);
        Assert.Equal("ZGF.ILSpy.Tests.pdb", Trees.TestAssembly.PdbFileName);
    }

    [Fact]
    public void An_assembly_exports_as_a_project_with_a_file_per_type()
    {
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));

        var exported = Trees.TestAssembly.ExportProject(DecompilerOptions.Default, projectPath);

        Assert.Equal(projectPath, exported.ProjectPath);
        Assert.Contains("<Project", File.ReadAllText(projectPath));
        var files = Directory.EnumerateFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories);
        Assert.Contains(files, f => Path.GetFileName(f) == "Calculator.cs");
    }

    [Fact]
    public void A_solution_holds_a_project_of_each_assembly_in_a_folder_of_its_own()
    {
        var (list, _) = Trees.OpenTestAssembly();
        var solutionPath = _temp.PathOf("All.sln");

        var exported = list.ExportSolution(DecompilerOptions.Default, solutionPath);

        var project = Assert.Single(exported);
        Assert.Equal(_temp.PathOf(Path.Combine("ZGF.ILSpy.Tests", "ZGF.ILSpy.Tests.csproj")), project.ProjectPath);
        Assert.True(File.Exists(project.ProjectPath));
        Assert.Contains("ZGF.ILSpy.Tests.csproj", File.ReadAllText(solutionPath));
    }

    [Fact]
    public void A_generated_pdb_embeds_the_decompiled_code()
    {
        var assembly = Trees.TestAssembly;
        var path = _temp.PathOf("Tests.pdb");

        Assert.True(assembly.CanGeneratePdb);
        assembly.GeneratePdb(DecompilerOptions.Default, path);

        using var stream = File.OpenRead(path);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var pdb = provider.GetMetadataReader();
        Assert.NotEmpty(pdb.Documents);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void The_project_export_toggles_are_offered_in_their_own_group()
    {
        var toggles = DecompilerToggle.All
            .Where(t => t.Group == DecompilerToggleGroup.ProjectExport)
            .Select(t => t.Name);

        Assert.Equal(["UseSdkStyleProjectFormat", "UseNestedDirectoriesForNamespaces"], toggles);
    }

    [Fact]
    public void The_project_export_toggles_leave_decompiled_code_as_it_was()
    {
        var nested = DecompilerToggle.Find("UseNestedDirectoriesForNamespaces")!;
        var changed = DecompilerOptions.Default.With(nested, !nested.DefaultValue);

        Assert.True(changed.ProducesSameCode(DecompilerOptions.Default, DecompilationLanguage.CSharp));
    }
}
