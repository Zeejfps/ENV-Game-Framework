using System.Text.RegularExpressions;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;

namespace ZGF.ILSpy.Tests;

public sealed class DiagramTests : IDisposable
{
    private const string FixturesNamespace = "ZGF.ILSpy.Tests.Fixtures";

    private static readonly Lazy<DiagramModel> s_model = new(() => ClassDiagrams.BuildModel(Trees.TestAssembly, null, CancellationToken.None));

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private static Dictionary<string, DiagramType> Fixtures => s_model.Value.TypesByNamespace[FixturesNamespace];

    private static DiagramType TypeNamed(string id) => Fixtures[id];

    [Fact]
    public void The_diagrammer_is_written_as_a_page_with_its_script_styles_and_icon()
    {
        var folder = _temp.PathOf("diagram");

        var page = ClassDiagrams.Write(Trees.TestAssembly, folder);

        Assert.Equal(Path.Combine(folder, ClassDiagrams.PageFileName), page);
        Assert.True(File.Exists(Path.Combine(folder, "script.js")));
        Assert.True(File.Exists(Path.Combine(folder, "styles.css")));
        Assert.True(File.Exists(Path.Combine(folder, "ILSpy.ico")));
        var html = File.ReadAllText(page);
        Assert.DoesNotMatch(new Regex(@"\{\{\w+\}\}"), html);
        Assert.Contains("<title>ZGF.ILSpy.Tests class diagrammer - ILSpy</title>", html);
        Assert.Contains("\"TypesByNamespace\":{", html);
    }

    [Fact]
    public void A_generic_class_is_drawn_with_its_members_and_named_with_its_type_parameters()
    {
        var calculator = TypeNamed("Calculator");

        Assert.Equal("Calculator❰T❱", calculator.Name);
        Assert.StartsWith("class Calculator {", calculator.Body);
        Assert.Contains("+int Count", calculator.Body);
        Assert.Contains("+Add(T item) void", calculator.Body);
        Assert.Contains("~Describe() string", calculator.Body);
        Assert.Contains("+int Answer$", calculator.Body);
        Assert.DoesNotContain("op_Addition", calculator.Body);
        Assert.DoesNotContain("<Count>k__BackingField", calculator.Body);
    }

    [Fact]
    public void An_enum_lists_its_values_and_an_interface_is_annotated()
    {
        var mode = Fixtures.Values.Single(t => t.Body.Contains("Precise"));
        var shape = TypeNamed("IShape");

        Assert.Contains("<<Enumeration>>\n    Fast\n    Slow\n    Precise", mode.Body);
        Assert.Contains("<<Interface>>", shape.Body);
        Assert.Contains("+double Area*", shape.Body);
    }

    [Fact]
    public void The_xml_documentation_of_types_and_members_is_carried_as_plain_text()
    {
        var calculator = TypeNamed("Calculator");

        Assert.Equal("How many items were added.", calculator.XmlDocs!["Count"]);
        Assert.StartsWith("Adds an item and raises [ZGF.ILSpy.Tests.Fixtures.Calculator`1.Changed].", calculator.XmlDocs["Add"]);
        Assert.Equal("The space the shape covers.", TypeNamed("IShape").XmlDocs!["Area"]);
    }

    [Fact]
    public void Types_outside_the_assembly_that_its_types_derive_from_are_named_as_outside_references()
    {
        var references = s_model.Value.OutsideReferences;

        Assert.Equal("System.IDisposable", references["System_IDisposable"]);
        Assert.Equal("System.IProgress❰T❱", references["System_IProgress_T"]);
    }

    [Fact]
    public void Building_stops_when_cancelled()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();

        Assert.Throws<OperationCanceledException>(() => ClassDiagrams.Write(Trees.TestAssembly, _temp.PathOf("cancelled"), null, cancel.Token));
        Assert.False(Directory.Exists(_temp.PathOf("cancelled")));
    }

    [Fact]
    public void Progress_counts_up_to_every_type()
    {
        var reports = new List<ExportProgress>();
        var progress = new Progress(reports);

        ClassDiagrams.BuildModel(Trees.TestAssembly, progress, CancellationToken.None);

        Assert.Equal(1f, reports[^1].Fraction);
        Assert.True(reports.Count > 1);
    }

    private sealed class Progress(List<ExportProgress> reports) : IProgress<ExportProgress>
    {
        public void Report(ExportProgress value) => reports.Add(value);
    }
}
