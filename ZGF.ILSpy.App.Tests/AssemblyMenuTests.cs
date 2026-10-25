using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Navigation;

namespace ZGF.ILSpy.App.Tests;

public sealed class AssemblyMenuTests
{
    private static readonly string CoreLibPath = typeof(object).Assembly.Location;

    [Fact]
    public void The_file_menu_sorts_the_list_by_name_and_closes_the_files_that_failed_to_load()
    {
        using var temp = new TempDir();
        var missing = temp.PathOf("Missing.dll");
        var app = AppDriver.Opened();
        OpenAssemblies(app.AppState, [missing, CoreLibPath]);
        app.RunUntil(() => app.AppState.Assemblies.Listed.Count == 3);
        app.Settle(2);

        app.Click("File").Frame(2);
        app.ClickMenuItem("Sort list by name").Frame(2);

        var sorted = app.AppState.Assemblies.FilePaths;
        Assert.Equal([missing, CoreLibPath, AppStateDriver.FixturePath], sorted);

        app.Click("File").Frame(2);
        app.ClickMenuItem("Close assemblies that failed to load").Frame(2);

        var roots = app.Ui.Tree.Roots;
        Assert.Equal([CoreLibPath, AppStateDriver.FixturePath], roots.Select(r => r.Assembly.FilePath));
        Assert.False(AnyFailedToLoad(app.AppState));
    }

    private static void PickFromTreeMenu(AppDriver app, string row, string item)
    {
        var at = app.TextRect(row);
        app.Frames.Click(at.X + 2f, at.Y + at.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem(item).Frame();
    }

    [Fact]
    public void The_tree_menu_shows_an_assembly_selected_in_its_folder()
    {
        var app = AppDriver.Opened();
        app.Run(2);
        var assembly = app.AppState.Assemblies.Assemblies[0];

        PickFromTreeMenu(app, assembly.Name, "Open containing folder");

        Assert.Equal([assembly.FilePath], app.Frames.RevealedFiles);
    }

    [Fact]
    public void The_tree_menu_opens_the_api_reference_page_of_a_public_type_and_offers_none_for_an_internal_one()
    {
        var app = AppDriver.Opened();
        var greeter = app.Fixture("Greeter");
        var hidden = app.Fixture("Hidden");
        app.Ui.Tree.Reveal(hidden);
        app.Ui.Tree.Reveal(greeter);
        app.Run(2);

        PickFromTreeMenu(app, "Greeter", "Show in .NET API reference");
        var row = app.TextRect("Hidden");
        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();

        Assert.Equal([MicrosoftDocs.FindPage(greeter)], app.Frames.OpenedUrls);
        Assert.EndsWith("/zgf.ilspy.app.tests.fixtures.greeter", app.Frames.OpenedUrls[0]);
        Assert.True(app.Frames.Drew("Copy full name"));
        Assert.False(app.Frames.Drew("Show in .NET API reference"));
    }

    [Fact]
    public void A_reference_in_code_opens_its_api_reference_page_and_a_private_one_reports_it_has_none()
    {
        var app = AppDriver.Opened();
        app.Show(app.Fixture("Greeter"), 2);
        app.Settle(2);
        var document = app.AppState.Tabs.Active!.Document;
        var references = document.SpansOf()
            .Select(s => s.Span.Reference)
            .Where(r => r is { IsNavigable: true, Kind: CodeReferenceKind.Type })
            .ToList();
        var greeter = references.First(r => r!.Name == "Greeter")!;
        var internalType = app.Fixture("Hidden").Reference!;

        ShowInApiReference(app.AppState, greeter);
        app.RunUntil(() => app.Frames.OpenedUrls.Count == 1);
        ShowInApiReference(app.AppState, internalType);
        app.RunUntil(() => app.AppState.Errors.Entries.Count == 1);

        Assert.EndsWith("/zgf.ilspy.app.tests.fixtures.greeter", app.Frames.OpenedUrls[0]);
        Assert.StartsWith("Hidden has no page in the .NET API reference", app.AppState.Errors.Entries[0].FullText);
    }

    [Fact]
    public void The_tree_menu_loads_the_dependencies_of_an_assembly()
    {
        var app = AppDriver.Opened();
        app.Run(2);
        var name = app.AppState.Assemblies.Assemblies[0].Name;
        var row = app.TextRect(name);

        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Load dependencies").Frame();
        app.RunUntil(() => app.AppState.Assemblies.Assemblies.Any(a => a.Name == "System.Runtime"));
        app.Settle(2);

        var names = app.Ui.Tree.Roots.Select(r => r.Assembly.Name).ToList();
        Assert.Equal(name, names[0]);
        Assert.Contains("System.Runtime", names);
        Assert.Contains("ZGF.ILSpy.Core", names);
    }
}
