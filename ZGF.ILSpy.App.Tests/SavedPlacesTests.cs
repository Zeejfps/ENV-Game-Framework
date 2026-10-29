using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Bookmarks;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Tabs and bookmarks as settings files hold them now, each step down the tree a node kind
/// and label, which later versions must still find their nodes by.</summary>
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
    ];

    private static string FixturePath => AppStateDriver.FixturePath.Replace(@"\", @"\\");

    private AppStateDriver Restored(string json)
    {
        var (settings, problem) = AppSettings.Load(_temp.Write("settings.json", json));
        Assert.IsType<SettingsProblem.None>(problem);
        var d = new AppStateDriver(settings);
        RestoreActiveAssemblyList(d.AppState);
        d.Settle();
        return d;
    }

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
}
