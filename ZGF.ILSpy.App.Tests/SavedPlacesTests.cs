using System.Text.Json;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Bookmarks;

namespace ZGF.ILSpy.App.Tests;

/// <summary>Tabs and bookmarks as settings files hold them, each with the versioned path to its node.</summary>
public sealed class SavedPlacesTests : IDisposable
{
    private const string Fixtures = AppStateDriver.FixturesNamespace;

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private const string GreeterPath = $$"""
        { "Version": 1, "Steps": [
            { "Step": "Namespace {{Fixtures}}", "Label": "{{Fixtures}}" },
            { "Step": "Definition T:{{Fixtures}}.Greeter", "Label": "Greeter" } ] }
        """;

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

    /// <summary>Settings listing the fixtures, with one tab whose place the file holds as <paramref name="place"/>.</summary>
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

    [Fact]
    public void A_tab_saved_with_a_versioned_path_shows_its_node_again()
    {
        var d = Restored(WithTab($"\"Path\": {GreeterPath}"));

        Assert.Same(d.Fixture("Greeter"), Assert.Single(d.AppState.Tabs).Node);
    }

    [Fact]
    public void A_bookmark_saved_with_a_versioned_path_goes_to_its_node_again()
    {
        var d = Restored(WithBookmark($"\"Path\": {GreeterPath}"));

        GoToBookmark(d.AppState, Assert.Single(d.AppState.Bookmarks.Items));
        d.Settle();

        Assert.Same(d.Fixture("Greeter"), d.AppState.Tabs.Active?.Node);
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
        var (d, problem) = RestoredWith(WithTab($"\"Path\": {path}"));

        Assert.Equal(["AssemblyLists"], Assert.IsType<SettingsProblem.ValuesLeftOut>(problem).Fields);
        Assert.Null(Assert.Single(d.AppState.Tabs).Node);
    }

    [Fact]
    public void A_bookmark_whose_path_cannot_be_read_is_reported_and_left_out()
    {
        var (d, problem) = RestoredWith(WithBookmark("\"Path\": { \"Version\": 2, \"Steps\": [] }"));

        Assert.Equal(["Bookmarks"], Assert.IsType<SettingsProblem.ValuesLeftOut>(problem).Fields);
        Assert.Empty(d.AppState.Bookmarks.Items);
    }

    [Fact]
    public void A_bookmark_whose_node_is_not_found_keeps_its_path_as_read()
    {
        const string missingPath = $$"""
            { "Version": 1, "Steps": [ { "Step": "Namespace {{Fixtures}}", "Label": "{{Fixtures}}" }, { "Step": "Definition T:{{Fixtures}}.Missing", "Label": "Missing" } ] }
            """;
        var d = Restored(WithBookmark($"\"Path\": {missingPath}"));

        GoToBookmark(d.AppState, Assert.Single(d.AppState.Bookmarks.Items));
        d.Settle();
        var saved = Saved(d).GetProperty("Bookmarks")[0];

        Assert.Equal([$"Namespace {Fixtures}", $"Definition T:{Fixtures}.Missing"], StepsOf(saved.GetProperty("Path")));
    }
}
