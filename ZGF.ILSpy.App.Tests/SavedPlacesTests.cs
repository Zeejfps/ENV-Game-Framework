using System.Text.Json;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Bookmarks;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Tabs and bookmarks as settings files hold them: as builds before versioned paths wrote them, each
/// step down the tree a node kind and label, which later builds must still find their nodes by; and with the
/// versioned path this build writes beside that.</summary>
public sealed class SavedPlacesTests : IDisposable
{
    private const string Fixtures = AppStateDriver.FixturesNamespace;
    private const string FixturesStep = $$"""{ "Kind": "Namespace", "Text": "{{Fixtures}}" }""";
    private const string GreeterStep = $$"""{ "Kind": "Class", "Text": "Greeter" }""";

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    public static IEnumerable<object[]> Places() =>
    [
        ["[]", Array.Empty<string>()],
        [$"[ {FixturesStep} ]", new[] { Fixtures }],
        [$"[ {FixturesStep}, {GreeterStep} ]", new[] { Fixtures, "Greeter" }],
        [$$"""[ {{FixturesStep}}, {{GreeterStep}}, { "Kind": "Method", "Text": "Greet(int) : string" } ]""", new[] { Fixtures, "Greeter", "Greet(int) : string" }],
        [$$"""[ {{FixturesStep}}, {{GreeterStep}}, { "Kind": "Field", "Text": "Name : string" } ]""", new[] { Fixtures, "Greeter", "Name : string" }],
        [$$"""[ {{FixturesStep}}, {{GreeterStep}}, { "Kind": "BaseTypes", "Text": "Base Types" }, { "Kind": "BaseType", "Text": "object" } ]""",
            new[] { Fixtures, "Greeter", "Base Types", "object" }],
        ["""[ { "Kind": "ReferencesFolder", "Text": "References" }, { "Kind": "AssemblyReference", "Text": "System.Runtime" } ]""",
            new[] { "References", "System.Runtime" }],
        ["""[ { "Kind": "ResourcesFolder", "Text": "Resources" }, { "Kind": "Resource", "Text": "data.json" } ]""", new[] { "Resources", "data.json" }],
        ["""[ { "Kind": "ResourcesFolder", "Text": "Resources" }, { "Kind": "ResourceImage", "Text": "logo.png" } ]""", new[] { "Resources", "logo.png" }],
        ["""[ { "Kind": "Metadata", "Text": "Metadata" } ]""", new[] { "Metadata" }],
        [$$"""[ {{FixturesStep}}, { "Kind": "Interface", "Text": "IDriven" } ]""", new[] { Fixtures, "IDriven" }],
        [$$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Vehicle" }, { "Kind": "Property", "Text": "Name : string" } ]""",
            new[] { Fixtures, "Vehicle", "Name : string" }],
        [$$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Counter" }, { "Kind": "Event", "Text": "Changed : Action?" } ]""",
            new[] { Fixtures, "Counter", "Changed : Action?" }],
        [$$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Outline" }, { "Kind": "Class", "Text": "Inner" } ]""", new[] { Fixtures, "Outline", "Inner" }],
        [$$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Vehicle" }, { "Kind": "DerivedTypes", "Text": "Derived Types" }, { "Kind": "DerivedType", "Text": "Car" } ]""",
            new[] { Fixtures, "Vehicle", "Derived Types", "Car" }],
        ["""[ { "Kind": "Metadata", "Text": "Metadata" }, { "Kind": "MetadataHeader", "Text": "DOS Header" } ]""", new[] { "Metadata", "DOS Header" }],
        ["""[ { "Kind": "Metadata", "Text": "Metadata" }, { "Kind": "MetadataDirectory", "Text": "Data Directories" } ]""", new[] { "Metadata", "Data Directories" }],
        ["""[ { "Kind": "Metadata", "Text": "Metadata" }, { "Kind": "MetadataTables", "Text": "Tables" } ]""", new[] { "Metadata", "Tables" }],
        ["""[ { "Kind": "ReferencesFolder", "Text": "References" }, { "Kind": "AssemblyReference", "Text": "System.Runtime" }, { "Kind": "ReferencedTypes", "Text": "Referenced types" } ]""",
            new[] { "References", "System.Runtime", "Referenced types" }],
    ];

    private const string GreeterPath = $$"""
        { "Version": 1, "Steps": [
            { "Step": "Namespace {{Fixtures}}", "Label": "{{Fixtures}}" },
            { "Step": "Definition T:{{Fixtures}}.Greeter", "Label": "Greeter" } ] }
        """;

    private const string FarewellPlace = $$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Farewell" } ]""";

    private static string FixturePath => AppStateDriver.FixturePath.Replace(@"\", @"\\");

    private AppStateDriver Restored(string json)
    {
        var (d, problem) = RestoredWith(json);
        Assert.IsType<SettingsProblem.None>(problem);
        return d;
    }

    private (AppStateDriver, SettingsProblem) RestoredWith(string json)
    {
        var (settings, problem) = AppSettings.Load(_temp.Write("settings.json", json));
        var d = new AppStateDriver(settings);
        RestoreActiveAssemblyList(d.AppState);
        d.Settle();
        return (d, problem);
    }

    /// <summary>Settings listing the fixtures, with one tab whose place the file holds as <paramref name="place"/>:
    /// a <c>Place</c>, a <c>Path</c>, or both.</summary>
    private static string WithTab(string place) => $$"""
        { "ActiveAssemblyList": "Default",
          "AssemblyLists": [ { "Name": "Default", "Assemblies": [ "{{FixturePath}}" ],
            "Documents": { "Active": "document-1", "Tabs": [
              { "kind": "node", "Panel": "document-1", "Title": "Saved", "FilePath": "{{FixturePath}}", {{place}}, "Language": "CSharp" } ] } } ] }
        """;

    /// <summary>Settings listing the fixtures, with one bookmark whose place the file holds as <paramref name="place"/>.</summary>
    private static string WithBookmark(string place) => $$"""
        { "ActiveAssemblyList": "Default",
          "AssemblyLists": [ { "Name": "Default", "Assemblies": [ "{{FixturePath}}" ] } ],
          "Bookmarks": [ { "Name": "Saved", "Enabled": true, "FileName": "{{FixturePath}}", "Kind": "Line", "LineNumber": 1,
            {{place}}, "Language": "CSharp" } ] }
        """;

    /// <summary>The settings file as <paramref name="d"/> saves it now.</summary>
    private JsonElement Saved(AppStateDriver d)
    {
        Settings.SaveSettings(d.AppState);
        using var json = JsonDocument.Parse(File.ReadAllText(_temp.PathOf("settings.json")));
        return json.RootElement.Clone();
    }

    private static string[] StepsOf(JsonElement path) =>
        path.GetProperty("Steps").EnumerateArray().Select(static s => s.GetProperty("Step").GetString()!).ToArray();

    private static string[] KindsOf(JsonElement place) =>
        place.EnumerateArray().Select(static s => s.GetProperty("Kind").GetString()!).ToArray();

    [Theory]
    [MemberData(nameof(Places))]
    public void A_saved_tab_shows_its_node_again(string place, string[] labels)
    {
        var d = Restored($$"""
            { "ActiveAssemblyList": "Default",
              "AssemblyLists": [ { "Name": "Default", "Assemblies": [ "{{FixturePath}}" ],
                "Documents": { "Active": "document-1", "Tabs": [
                  { "kind": "node", "Panel": "document-1", "Title": "Saved", "FilePath": "{{FixturePath}}", "Place": {{place}}, "Language": "CSharp" } ] } } ] }
            """);

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Same(d.Node(labels), tab.Node);
    }

    [Theory]
    [MemberData(nameof(Places))]
    public void A_saved_bookmark_goes_to_its_node_again(string place, string[] labels)
    {
        var d = Restored($$"""
            { "ActiveAssemblyList": "Default",
              "AssemblyLists": [ { "Name": "Default", "Assemblies": [ "{{FixturePath}}" ] } ],
              "Bookmarks": [ { "Name": "Saved", "Enabled": true, "FileName": "{{FixturePath}}", "Kind": "Line", "LineNumber": 1,
                "Place": {{place}}, "Language": "CSharp" } ] }
            """);

        GoToBookmark(d.AppState, Assert.Single(d.AppState.Bookmarks.Items));
        d.Settle();

        Assert.Same(d.Node(labels), d.AppState.Tabs.Active?.Node);
    }

    [Fact]
    public void A_tab_saved_with_a_versioned_path_shows_its_node_again()
    {
        var d = Restored(WithTab($"\"Path\": {GreeterPath}"));

        Assert.Same(d.Fixture("Greeter"), Assert.Single(d.AppState.Tabs).Node);
    }

    [Fact]
    public void A_tab_saved_with_both_a_path_and_a_place_goes_where_the_path_says()
    {
        var d = Restored(WithTab($"\"Path\": {GreeterPath}, \"Place\": {FarewellPlace}"));

        Assert.Same(d.Fixture("Greeter"), Assert.Single(d.AppState.Tabs).Node);
    }

    [Theory]
    [InlineData("""{ "Version": 1, "Steps": [ { "Step": "Somewhere ZGF", "Label": "ZGF" } ] }""")]
    [InlineData("""{ "Version": 1, "Steps": [ { "Step": "Folder Nowhere", "Label": "Nowhere" } ] }""")]
    [InlineData("""{ "Version": 1, "Steps": [ { "Label": "Greeter" } ] }""")]
    [InlineData("""{ "Version": 1 }""")]
    [InlineData("""{ "Version": 2, "Steps": [] }""")]
    [InlineData("""{ "Steps": [] }""")]
    public void A_tab_whose_path_cannot_be_read_is_reported_and_left_out_rather_than_shown_at_the_root(string path)
    {
        var (d, problem) = RestoredWith(WithTab($"\"Path\": {path}, \"Place\": {FarewellPlace}"));

        Assert.Equal(["AssemblyLists"], Assert.IsType<SettingsProblem.ValuesLeftOut>(problem).Fields);
        Assert.Null(Assert.Single(d.AppState.Tabs).Node);
    }

    [Fact]
    public void A_bookmark_whose_path_cannot_be_read_is_reported_and_left_out()
    {
        var (d, problem) = RestoredWith(WithBookmark($"\"Path\": {{ \"Version\": 2, \"Steps\": [] }}, \"Place\": {FarewellPlace}"));

        Assert.Equal(["Bookmarks"], Assert.IsType<SettingsProblem.ValuesLeftOut>(problem).Fields);
        Assert.Empty(d.AppState.Bookmarks.Items);
    }

    [Fact]
    public void A_place_naming_a_kind_no_build_saved_is_reported_and_left_out()
    {
        var (d, problem) = RestoredWith(WithTab("\"Place\": [ { \"Kind\": \"Spaceship\", \"Text\": \"Greeter\" } ]"));

        Assert.Equal(["AssemblyLists"], Assert.IsType<SettingsProblem.ValuesLeftOut>(problem).Fields);
        Assert.Null(Assert.Single(d.AppState.Tabs).Node);
    }

    [Fact]
    public void A_tab_found_by_its_old_place_is_saved_with_a_versioned_path_alone()
    {
        var d = Restored(WithTab($"\"Place\": {FarewellPlace}"));

        var tab = Saved(d).GetProperty("AssemblyLists")[0].GetProperty("Documents").GetProperty("Tabs")[0];

        Assert.Equal(SavedPath.CurrentVersion, tab.GetProperty("Path").GetProperty("Version").GetInt32());
        Assert.Equal([$"Namespace {Fixtures}", $"Definition T:{Fixtures}.Farewell"], StepsOf(tab.GetProperty("Path")));
        Assert.Equal(JsonValueKind.Null, tab.GetProperty("Place").ValueKind);
    }

    [Fact]
    public void A_bookmark_keeps_its_old_place_as_read_until_it_is_gone_to_then_is_saved_with_a_versioned_path_alone()
    {
        var d = Restored(WithBookmark($"\"Place\": {FarewellPlace}"));

        var unvisited = Saved(d).GetProperty("Bookmarks")[0];
        GoToBookmark(d.AppState, Assert.Single(d.AppState.Bookmarks.Items));
        d.Settle();
        var visited = Saved(d).GetProperty("Bookmarks")[0];

        Assert.Equal(JsonValueKind.Null, unvisited.GetProperty("Path").ValueKind);
        Assert.Equal(["Namespace", "Class"], KindsOf(unvisited.GetProperty("Place")));
        Assert.Same(d.Fixture("Farewell"), d.AppState.Tabs.Active?.Node);
        Assert.Equal([$"Namespace {Fixtures}", $"Definition T:{Fixtures}.Farewell"], StepsOf(visited.GetProperty("Path")));
        Assert.Equal(JsonValueKind.Null, visited.GetProperty("Place").ValueKind);
    }

    [Fact]
    public void A_bookmark_whose_node_is_not_found_keeps_its_path_and_place_as_read()
    {
        const string missingPath = $$"""
            { "Version": 1, "Steps": [ { "Step": "Namespace {{Fixtures}}", "Label": "{{Fixtures}}" }, { "Step": "Definition T:{{Fixtures}}.Missing", "Label": "Missing" } ] }
            """;
        const string missingPlace = $$"""[ {{FixturesStep}}, { "Kind": "Class", "Text": "Missing" } ]""";
        var d = Restored(WithBookmark($"\"Path\": {missingPath}, \"Place\": {missingPlace}"));

        GoToBookmark(d.AppState, Assert.Single(d.AppState.Bookmarks.Items));
        d.Settle();
        var saved = Saved(d).GetProperty("Bookmarks")[0];

        Assert.Equal([$"Namespace {Fixtures}", $"Definition T:{Fixtures}.Missing"], StepsOf(saved.GetProperty("Path")));
        Assert.Equal(["Namespace", "Class"], KindsOf(saved.GetProperty("Place")));
        Assert.Equal("Missing", saved.GetProperty("Place")[1].GetProperty("Text").GetString());
    }
}
