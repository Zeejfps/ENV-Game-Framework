using System.Text.Json.Nodes;
using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Comparison;
using ZGF.ILSpy.Core.Diagrams;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.App.Tests.TestPaths;

namespace ZGF.ILSpy.App.Tests;

public sealed class SettingsFormatTests : IDisposable
{
    private const string SettingsFixture = "settings.json";
    private const string BookmarksFixture = "ILSpy.Bookmarks.json";

    private static readonly AssemblyPath A = PathOf(@"C:\Fixture\A.dll");
    private static readonly AssemblyPath B = PathOf(@"C:\Fixture\B.dll");
    private static readonly TypeAnchor Calculator = new(A, "Ns.Calculator");

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Settings_with_every_field_set_are_written_as_the_fixture()
    {
        var path = _temp.PathOf(SettingsFixture);
        var settings = ExpectedSettings();
        settings.FilePath = path;

        Assert.IsType<SettingsWrite.Written>(settings.Save());

        AssertSameJson(Fixture(SettingsFixture), File.ReadAllText(path));
    }

    [Fact]
    public void The_settings_fixture_loads_whole_and_is_saved_as_it_was()
    {
        var path = _temp.Write(SettingsFixture, Fixture(SettingsFixture));

        var (loaded, problem) = AppSettings.Load(path);
        Assert.IsType<SettingsProblem.None>(problem);
        Assert.IsType<SettingsWrite.Written>(loaded.Save());

        AssertSameJson(Fixture(SettingsFixture), File.ReadAllText(path));
    }

    [Fact]
    public void Bookmarks_with_every_field_set_are_written_as_the_fixture()
    {
        var path = _temp.PathOf(BookmarksFixture);
        Directory.CreateDirectory(_temp.Path);

        Assert.Null(BookmarkFile.Write(path, ExpectedBookmarks()));

        AssertSameJson(Fixture(BookmarksFixture), File.ReadAllText(path));
    }

    [Fact]
    public void The_bookmarks_fixture_reads_whole_and_is_written_as_it_was()
    {
        var path = _temp.Write(BookmarksFixture, Fixture(BookmarksFixture));

        var read = BookmarkFile.Read(path, out var error);
        Assert.Null(error);
        Assert.Empty(read!.Unreadable);
        Assert.Null(BookmarkFile.Write(path, read.Bookmarks));

        AssertSameJson(Fixture(BookmarksFixture), File.ReadAllText(path));
    }

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Settings", name));

    private static void AssertSameJson(string expected, string actual)
    {
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(expected), JsonNode.Parse(actual)), actual);
    }

    private static AppSettings ExpectedSettings() => new()
    {
        AssemblyLists =
        [
            new AssemblyListRecord("Work", [A, B],
                [new CodeAnchor(CodeAnchorKind.Statement, A, 0x06000001, 7)],
                new Dictionary<AssemblyPath, string> { [A] = @"C:\Fixture\A.pdb" },
                new Dictionary<AssemblyPath, string> { [B] = ".NETCoreApp,Version=v8.0" },
                ExpectedDocuments()),
            new AssemblyListRecord("Play", [B], []),
        ],
        ActiveAssemblyList = "Work",
        Theme = ThemeId<ThemeFile>.Of("monokai"),
        IconTheme = ThemeId<IconThemeFile>.Of("custom-icons"),
        Language = DecompilationLanguage.IL,
        Decompiler = new DecompilerOptions { LanguageVersion = CSharpVersion.CSharp12 }.With(DecompilerToggle.Find("AlwaysUseBraces")!, false),
        FoldedOptionGroups = ["Other"],
        Display = new DisplayOptions(14f, false, true, false, false, 1.25f, new CodeFont("Mono", @"C:\Fonts\Mono.ttf", 1),
            TokenFormat.Hex, true, true, true, false),
        Locale = Locale.De,
        ApiVisibility = ApiVisibility.All,
        SearchKinds = SearchKinds.Types | SearchKinds.Literals,
        Layout = new DockLayout
        {
            Root = new DockSplit
            {
                Id = 1, Direction = Direction.Horizontal, Ratio = 0.25f,
                First = new DockGroup { Id = 2, Panels = ["tree"], Active = "tree" },
                Second = new DockGroup { Id = 3, Panels = ["documents", "search"], Active = "search" },
            },
            Windows = [new DockWindow { Id = 4, Root = new DockGroup { Id = 5, Panels = ["analyzer"], Active = "analyzer" }, X = 10f, Y = 20f, Width = 300f, Height = 200f }],
            NextId = 6,
        },
        Bookmarks = ExpectedBookmarks(),
        BookmarkColumns = [1.5f, 2.5f],
        SearchColumns = [3.5f, 1f],
        ExportColumns = [2f, 0.5f],
        FlagsFilters = new Dictionary<string, SavedFilterState>
        {
            ["TypeDef/Attributes"] = new(new Dictionary<string, uint[]> { ["Visibility"] = [1u, 2u] },
                new Dictionary<string, TriState> { ["Sealed"] = TriState.Required }, MatchMode.Any),
        },
        Window = new WindowBounds(10, 20, 1200, 800, true),
        ExportWindow = new WindowBounds(30, 40, 900, 600, false),
        NuGet = new SavedNuGetSearch("https://api.nuget.org/v3/index.json", true),
        NuGetWindow = new WindowBounds(50, 60, 700, 500, false),
    };

    private static DocumentsRecord ExpectedDocuments()
    {
        var code = ViewPosition.OfCode(new CodeViewPosition(12, 3.5f, new TextPosition(13, 2), new TextPosition(14, 5)));
        var grid = ViewPosition.OfGrid(new GridPosition(4, 1.5f, 6, 2));
        var view = new DiagramViewRecord(10.5f, -20.25f, 1.5f, [CardFold.Own(Calculator, MemberSection.Methods), CardFold.Inherited(Calculator, "Base")], true);
        DocumentRecord[] tabs =
        [
            new NodeDocumentRecord("document-1", "Calculator", CalculatorPlace(), DecompilationLanguage.IL, code),
            new NodeDocumentRecord("document-2", "Other", OtherPlace(), DecompilationLanguage.ILWithCSharp, grid),
            new DiagramDocumentRecord("document-3", "Diagram", [Calculator, new TypeAnchor(B, "Ns.Other")], [A], view),
            new ComparisonDocumentRecord("document-4", "Comparison", A, B, ApiScope.AllDeclared, new ComparisonViewRecord(true)),
        ];
        var layout = new DockLayout
        {
            Root = new DockSplit
            {
                Id = 1, Direction = Direction.Horizontal, Ratio = 0.75f,
                First = new DockGroup { Id = 2, Panels = ["document-1", "document-2"], Active = "document-2" },
                Second = new DockGroup { Id = 3, Panels = ["document-3"], Active = "document-3" },
            },
            Windows = [new DockWindow { Id = 4, Root = new DockGroup { Id = 5, Panels = ["document-4"], Active = "document-4" }, X = 5f, Y = 6f, Width = 400f, Height = 300f }],
            NextId = 6,
        };
        return new DocumentsRecord(tabs.ToList(), "document-3", "document-4", layout);
    }

    private static List<BookmarkRecord> ExpectedBookmarks()
    {
        var view = new BookmarkViewState(2, 100, 50.5, 7.25, 1234, [new BookmarkFoldingRange(1, 9)],
            new BookmarkRenderedLayoutSettings(true, true, true, true, true, true, 8, 6), ["A", "Ns", "Calculator"]);
        return
        [
            new BookmarkRecord("Mark", true, A, "A, Version=1.0.0.0", "A.dll", 0x06000002, BookmarkKind.Body, 12, 34,
                "Ns.Calculator.Add", "Add(int, int) : int", view, CalculatorPlace(), DecompilationLanguage.IL),
            new BookmarkRecord("Old", true, B, "B, Version=2.0.0.0", "B.dll", 0x02000003, BookmarkKind.Line, 5, 56,
                "Ns.Other", "Other", null),
        ];
    }

    private static NodePath CalculatorPlace() => NodePath.From(A,
    [
        new NodePathStep(new NodePathSegment.Namespace("Ns"), "Ns"),
        new NodePathStep(new NodePathSegment.Definition("T:Ns.Calculator"), "Calculator"),
    ]);

    private static NodePath OtherPlace() => NodePath.From(B,
    [
        new NodePathStep(new NodePathSegment.Namespace("Ns"), "Ns"),
        new NodePathStep(new NodePathSegment.Definition("T:Ns.Other"), "Other"),
    ]);
}
