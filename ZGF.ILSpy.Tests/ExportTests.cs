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

    /// <summary>Every report, in the order they came, from whichever thread sent them.</summary>
    private sealed class RecordedProgress : IProgress<ExportProgress>
    {
        private readonly List<ExportProgress> _reports = new();

        public List<ExportProgress> Reports
        {
            get
            {
                lock (_reports)
                    return _reports.ToList();
            }
        }

        public void Report(ExportProgress value)
        {
            lock (_reports)
                _reports.Add(value);
        }
    }

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
        Assert.Equal("ZGF.ILSpy.Tests.pdb", Trees.TestAssembly.PdbFileName);
    }

    [Fact]
    public void An_assembly_exports_as_a_project_with_a_file_per_type()
    {
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));

        var exported = AssemblyExport.ExportProject(Trees.TestAssembly, DecompilerOptions.Default, projectPath);

        Assert.Equal(projectPath, exported.ProjectPath);
        Assert.Contains("<Project", File.ReadAllText(projectPath));
        var files = Directory.EnumerateFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories);
        Assert.Contains(files, f => Path.GetFileName(f) == "Calculator.cs");
    }

    [Fact]
    public void Baml_exports_as_xaml_pages_beside_their_code_behind()
    {
        var assembly = new AssemblyList().Open(Trees.WpfAssemblyPath);
        var projectPath = _temp.PathOf(Path.Combine("Wpf", "BamlFixture.csproj"));

        AssemblyExport.ExportProject(assembly, DecompilerOptions.Default, projectPath);

        var folder = Path.GetDirectoryName(projectPath)!;
        var project = File.ReadAllText(projectPath);
        var window = File.ReadAllText(Path.Combine(folder, "BamlFixture", "MainWindow.xaml"));
        var codeBehind = File.ReadAllText(Path.Combine(folder, "BamlFixture", "MainWindow.cs"));
        Assert.Contains("x:Class=\"BamlFixture.MainWindow\"", window);
        Assert.Contains("<Page Include=\"BamlFixture\\MainWindow.xaml\"", project);
        Assert.Contains("<ApplicationDefinition Include=\"BamlFixture\\App.xaml\"", project);
        Assert.DoesNotContain("baml", project);
        Assert.DoesNotContain("void InitializeComponent()", codeBehind);
    }

    [Fact]
    public void A_solution_holds_a_project_of_each_assembly_in_a_folder_of_its_own()
    {
        var solutionPath = _temp.PathOf("All.sln");

        var exported = AssemblyExport.ExportSolution(AssemblyExport.NameProjects([Trees.TestAssembly]), DecompilerOptions.Default, solutionPath);

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
        AssemblyExport.GeneratePdb(assembly, DecompilerOptions.Default, path);

        using var stream = File.OpenRead(path);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var pdb = provider.GetMetadataReader();
        Assert.NotEmpty(pdb.Documents);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void A_project_export_reports_its_files_as_it_writes_them()
    {
        var progress = new RecordedProgress();
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));

        AssemblyExport.ExportProject(Trees.TestAssembly, DecompilerOptions.Default, projectPath, progress: progress);

        var reports = progress.Reports;
        Assert.Equal(new ExportProgress("ZGF.ILSpy.Tests", 1, 1, 0f), reports[0]);
        Assert.Contains(reports, r => r.Fraction == 1f);
    }

    [Fact]
    public void A_solution_reports_each_project_as_a_part_of_the_whole()
    {
        var progress = new RecordedProgress();
        var solutionPath = _temp.PathOf("All.sln");

        AssemblyExport.ExportSolution(AssemblyExport.NameProjects([Trees.TestAssembly, Trees.TestAssembly]), DecompilerOptions.Default, solutionPath, progress: progress);

        Assert.Contains(progress.Reports, r => r is { Part: 1, Parts: 2 });
        Assert.Contains(progress.Reports, r => r is { Part: 2, Parts: 2, Fraction: 0.5f });
        Assert.All(progress.Reports, r => Assert.InRange(r.Fraction!.Value, 0f, 1f));
    }

    [Fact]
    public void A_solution_puts_each_project_in_a_folder_named_as_the_project()
    {
        var solutionPath = _temp.PathOf("All.sln");
        SolutionProject[] projects = [new(Trees.TestAssembly, "Renamed")];

        var exported = AssemblyExport.ExportSolution(projects, DecompilerOptions.Default, solutionPath);

        var project = Assert.Single(exported);
        Assert.Equal(_temp.PathOf(Path.Combine("Renamed", "Renamed.csproj")), project.ProjectPath);
        Assert.Contains(Path.Combine("Renamed", "Renamed.csproj"), File.ReadAllText(solutionPath));
    }

    [Fact]
    public void Projects_are_named_after_their_assemblies_with_a_number_after_a_taken_name()
    {
        var projects = AssemblyExport.NameProjects([Trees.TestAssembly, Trees.TestAssembly]);

        Assert.Equal(["ZGF.ILSpy.Tests", "ZGF.ILSpy.Tests_2"], projects.Select(p => p.Name));
    }

    [Theory]
    [InlineData("Mine", "Mine.sln")]
    [InlineData("Mine.sln", "Mine.sln")]
    [InlineData(" Mine.SLN ", "Mine.sln")]
    public void A_solution_file_is_named_with_one_extension(string typed, string expected)
    {
        Assert.Equal(expected, AssemblyExport.SolutionFileName(typed));
    }

    [Fact]
    public void Each_assembly_takes_the_share_of_the_export_its_types_make_up()
    {
        var coreLib = new AssemblyList().Open(Trees.CoreLibPath);
        var tests = Trees.TestAssembly;

        var bounds = AssemblyExport.PartBounds([tests, coreLib]);

        var testTypes = tests.File.Metadata.TypeDefinitions.Count;
        var coreLibTypes = coreLib.File.Metadata.TypeDefinitions.Count;
        Assert.Equal(0f, bounds[0]);
        Assert.Equal((float)testTypes / (testTypes + coreLibTypes), bounds[1], 5);
        Assert.Equal(1f, bounds[2]);
    }

    [Fact]
    public void A_cancelled_save_leaves_no_file_behind()
    {
        var path = _temp.PathOf("Tests.cs");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            Trees.TestAssembly.Root.SaveCode(DecompilationLanguage.CSharp, DecompilerOptions.Default, path, cancel.Token));

        Assert.Empty(Directory.EnumerateFileSystemEntries(_temp.Path));
    }

    [Fact]
    public void A_cancelled_project_export_stops()
    {
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            AssemblyExport.ExportProject(Trees.TestAssembly, DecompilerOptions.Default, projectPath, cancellationToken: cancel.Token));
    }

    private static readonly Guid EmbeddedSource = new("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

    private static int CountEmbeddedSources(string pdbPath)
    {
        using var stream = File.OpenRead(pdbPath);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var pdb = provider.GetMetadataReader();
        return pdb.CustomDebugInformation
            .Select(h => pdb.GetCustomDebugInformation(h))
            .Count(i => pdb.GetGuid(i.Kind) == EmbeddedSource);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_project_gets_a_pdb_beside_it_with_or_without_the_sources_in_it(bool embed)
    {
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));

        var exported = AssemblyExport.ExportProject(Trees.TestAssembly, DecompilerOptions.Default, projectPath, new ProjectExtras(Pdb: true, EmbedSourcesInPdb: embed));

        Assert.Null(exported.PdbFailure);
        Assert.Equal(_temp.PathOf(Path.Combine("Project", "ZGF.ILSpy.Tests.pdb")), exported.PdbPath);
        Assert.Equal(embed, CountEmbeddedSources(exported.PdbPath!) > 0);
    }

    [Fact]
    public void A_strong_name_key_is_copied_into_the_project_and_signs_it()
    {
        var key = _temp.Write("keys/signing.snk", [1, 2, 3]);
        var projectPath = _temp.PathOf(Path.Combine("Project", "Tests.csproj"));

        AssemblyExport.ExportProject(Trees.TestAssembly, DecompilerOptions.Default, projectPath, new ProjectExtras(StrongNameKeyFile: key));

        Assert.True(File.Exists(_temp.PathOf(Path.Combine("Project", "signing.snk"))));
        Assert.Contains("signing.snk", File.ReadAllText(projectPath));
    }

    [Fact]
    public void Resetting_the_code_toggles_leaves_how_projects_are_exported()
    {
        var nested = DecompilerToggle.Find("UseNestedDirectoriesForNamespaces")!;
        var deadCode = DecompilerToggle.Find("RemoveDeadCode")!;
        var changed = DecompilerOptions.Default
            .With(nested, !nested.DefaultValue)
            .With(deadCode, !deadCode.DefaultValue);

        var reset = changed.WithCodeTogglesReset();

        Assert.Equal(!nested.DefaultValue, reset.IsOn(nested));
        Assert.Equal(deadCode.DefaultValue, reset.IsOn(deadCode));
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
