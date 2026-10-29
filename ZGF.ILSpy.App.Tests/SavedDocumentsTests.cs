using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Diagrams;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Diagrams;
using static ZGF.ILSpy.DocumentPanels;
using static ZGF.ILSpy.Navigation;
using static ZGF.ILSpy.App.Tests.TestPaths;

namespace ZGF.ILSpy.App.Tests;

public sealed class SavedDocumentsTests : IDisposable
{
    private const string Other = "Other";
    private const string MissingPanel = "document-5";
    private const string DiagramPanel = "document-2";

    private readonly TempDir _temp = new();

    public void Dispose() => _temp.Dispose();

    private static CodeViewState CodeOf(AppDriver app, DocumentTab tab) => app.Ui.Documents.ViewOf(tab).Code;

    private static DiagramViewState DiagramOf(AppDriver app, DocumentTab tab) => app.Ui.Documents.ViewOf(tab).Diagram;

    private static DocumentTab TabTitled(AppDriver app, string title) => app.AppState.Tabs.Single(t => t.Title == title);

    private static CodeViewPosition ScrollTo(AppDriver app, DocumentTab tab, int topLine, TextPosition anchor, TextPosition caret)
    {
        CodeOf(app, tab).Restore(new CodeViewPosition(topLine, 0f, anchor, caret));
        app.Run(4);
        return CodeOf(app, tab).Position;
    }

    /// <summary>Docks <paramref name="tab"/>'s panel in a group of its own right of <paramref name="beside"/>'s.</summary>
    private static void MoveRightOf(AppDriver app, DocumentTab tab, DocumentTab beside)
    {
        var layout = app.Ui.Layout;
        var group = layout.GroupOf(app.Ui.Documents.PanelOf(beside))!;
        layout.Dock(app.Ui.Documents.PanelOf(tab), group, DockEdge.Right);
        app.Run(2);
    }

    /// <summary>Runs frames until the diagram of <paramref name="tab"/> is laid out and its cards stopped moving.</summary>
    private static void SettleDiagram(AppDriver app, DocumentTab tab)
    {
        app.RunUntil(() => IsLaidOut(app, tab));
        app.Run();
    }

    private static bool IsLaidOut(AppDriver app, DocumentTab tab)
    {
        if (app.AppState.IsDiagramming)
            return false;
        var view = DiagramOf(app, tab);
        if (view.LaidOutVersion != tab.Diagram!.Version)
            return false;
        if (view.IsFitPending)
            return false;
        return !view.Transition.IsMoving;
    }

    /// <summary>Each open tab's title with the panel it is docked as, in the order the tabs opened.</summary>
    private static List<(string? Title, string Panel)> ListTabPanels(AppDriver app)
    {
        return app.AppState.Tabs
            .Select(t => (t.Title, app.Ui.Documents.PanelOf(t)))
            .ToList();
    }

    /// <summary>Each group of the layout as its panels and the one in front.</summary>
    private static List<string> ListGroups(AppDriver app)
    {
        return app.Ui.Layout.AllGroups()
            .Select(static g => $"{string.Join(",", g.Panels)} [{g.Active}]")
            .ToList();
    }

    private static List<TypeAnchor> ListTypes(DocumentTab tab)
    {
        return tab.Diagram!.Cards
            .Select(static c => c.Anchor)
            .ToList();
    }

    private static AppSettings SettingsWith(DocumentsRecord documents)
    {
        var list = new AssemblyListRecord(SavedAssemblyLists.DefaultName, [PathOf(AppStateDriver.FixturePath)], Documents: documents);
        return new AppSettings { AssemblyLists = [list] };
    }

    private static TypeAnchor FixtureType(string name) => new(PathOf(AppStateDriver.FixturePath), $"{AppStateDriver.FixturesNamespace}.{name}");

    /// <summary>Where the fixture type <paramref name="name"/> is, as this build saves it.</summary>
    private static NodePath FixturePlace(string name) => NodePath.From(PathOf(AppStateDriver.FixturePath),
    [
        new NodePathStep(new NodePathSegment.Namespace(AppStateDriver.FixturesNamespace), AppStateDriver.FixturesNamespace),
        new NodePathStep(new NodePathSegment.Definition($"T:{AppStateDriver.FixturesNamespace}.{name}"), name),
    ]);

    [Fact]
    public void Scrolling_alone_writes_no_settings_and_the_next_save_keeps_where_the_view_stands()
    {
        var app = new AppDriver(height: 360f, settings: _temp.Settings());
        app.Driver.OpenFixtures();
        app.Run(3);
        app.Show(app.Fixture("Outline"), 4);
        var outline = app.AppState.Tabs.Active!;
        app.Run(3);
        var file = _temp.PathOf("settings.json");
        var before = File.ReadAllText(file);

        var position = ScrollTo(app, outline, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        app.Run(3);
        Assert.Equal(before, File.ReadAllText(file));

        app.App.SaveOnExit(new WindowBounds(10, 20, 1000, 360, false));
        var next = new AppDriver(height: 360f, settings: _temp.Settings());
        RestoreActiveAssemblyList(next.AppState);
        next.Settle(4);
        Assert.Equal(position, CodeOf(next, TabTitled(next, "Outline")).Position);
    }

    [Fact]
    public void After_a_restart_the_tabs_come_back_in_their_panels_and_groups_with_their_views_where_they_were()
    {
        var app = new AppDriver(height: 360f, settings: _temp.Settings());
        app.Driver.OpenFixtures();
        app.Run(3);
        app.Show(app.Fixture("Outline"), 4);
        var outline = app.AppState.Tabs.Active!;
        KeepTabOpen(app.AppState, outline);
        var outlinePosition = ScrollTo(app, outline, 20, new TextPosition(21, 4), new TextPosition(22, 8));
        app.Show(app.Fixture("Greeter"), 4, inNewTab: true);
        var greeter = app.AppState.Tabs.Active!;
        MoveRightOf(app, greeter, outline);
        app.Show(app.Fixture("Farewell"), 4);
        ShowInNewDiagram(app.AppState, app.Fixture("Vehicle").Definition, Names.Diagram(app.Fixture("Vehicle").Definition));
        app.Settle(2);
        var diagram = app.AppState.Tabs.Active!;
        SettleDiagram(app, diagram);
        var view = DiagramOf(app, diagram);
        view.Camera.Restore(new PointF(-30f, 12f), 1.5f);
        var fields = CardFold.Own(diagram.Diagram!.Cards[0].Anchor, MemberSection.Fields);
        view.Folds.Toggle(fields);
        view.ShowHiddenMembers(true);
        ActivateTab(app.Ui, greeter);
        app.Run(3);
        app.App.SaveOnExit(new WindowBounds(10, 20, 1000, 360, false));

        var next = new AppDriver(height: 360f, settings: _temp.Settings());
        RestoreActiveAssemblyList(next.AppState);
        next.Settle(4);

        Assert.Equal(ListTabPanels(app), ListTabPanels(next));
        Assert.Equal(ListGroups(app), ListGroups(next));
        Assert.Equal("Greeter", next.AppState.Tabs.Active!.Title);
        Assert.Equal("Farewell", next.AppState.Tabs.Preview!.Title);
        Assert.Equal("Farewell", TabTitled(next, "Farewell").Node!.Text);
        Assert.Equal(outlinePosition, CodeOf(next, TabTitled(next, "Outline")).Position);
        var restored = TabTitled(next, diagram.Title!);
        var restoredView = DiagramOf(next, restored);
        Assert.Equal(ListTypes(diagram), ListTypes(restored));
        Assert.Equal(new PointF(-30f, 12f), restoredView.Camera.Corner);
        Assert.Equal(1.5f, restoredView.Camera.Zoom);
        Assert.False(restoredView.Folds.IsOpen(fields));
        Assert.True(restoredView.ShowsHiddenMembers);
    }

    [Fact]
    public void A_tab_whose_node_is_gone_after_a_restart_says_so_in_its_panel()
    {
        var missing = new NodeDocumentRecord(MissingPanel, "Missing", FixturePlace("Missing"), null, DecompilationLanguage.CSharp, null);
        var d = new AppStateDriver(SettingsWith(new DocumentsRecord([missing], MissingPanel, null, null)));

        RestoreActiveAssemblyList(d.AppState);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        var gone = Assert.IsType<TabContent.Gone>(tab.Content);
        Assert.Equal("Missing", gone.Location.Label);
        Assert.Equal(MissingPanel, d.Ui.Documents.PanelOf(tab));
        Assert.True(d.Ui.Layout.Contains(MissingPanel));
    }

    [Fact]
    public void Clearing_the_list_before_it_reopens_settles_its_tabs_rather_than_leaving_them_reloading()
    {
        var greeter = new NodeDocumentRecord(MissingPanel, "Greeter", FixturePlace("Greeter"), null, DecompilationLanguage.CSharp, null);
        var d = new AppStateDriver(SettingsWith(new DocumentsRecord([greeter], MissingPanel, null, null)));

        RestoreActiveAssemblyList(d.AppState);
        CloseAllAssemblies(d.AppState);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.IsType<TabContent.Gone>(tab.Content);
    }

    [Fact]
    public void A_diagram_comes_back_without_the_types_no_longer_found_and_is_the_one_shown_types_go_to()
    {
        var vehicle = FixtureType("Vehicle");
        var saved = new DiagramDocumentRecord(DiagramPanel, "Vehicle diagram", [vehicle, FixtureType("Missing")], [PathOf(AppStateDriver.FixturePath)], null);
        var d = new AppStateDriver(SettingsWith(new DocumentsRecord([saved], DiagramPanel, null, null)));

        RestoreActiveAssemblyList(d.AppState);
        d.Settle();

        var tab = Assert.Single(d.AppState.Tabs);
        Assert.Equal("Vehicle diagram", tab.Title);
        Assert.Equal([vehicle], ListTypes(tab));
        Assert.True(tab.Diagram!.IsHome(PathOf(AppStateDriver.FixturePath)));
        Assert.Same(tab, d.AppState.Tabs.Diagram);
    }

    [Fact]
    public void Switching_lists_swaps_their_documents()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.AppState.Lists.Create(Other);
        SwitchAssemblyList(d.AppState, Other);
        d.Settle();
        OpenAssembly(d.AppState, AppStateDriver.FixturePath);
        d.RunUntil(() => d.AppState.Assemblies.FilePaths.Contains(PathOf(AppStateDriver.FixturePath)));
        d.Settle();
        d.Show(d.Fixture("Farewell"));

        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        d.Settle();
        var first = Assert.Single(d.AppState.Tabs);
        var firstPanel = d.Ui.Documents.PanelOf(first);
        var firstDocuments = d.Ui.MainLayout.ListDocuments();
        SwitchAssemblyList(d.AppState, Other);
        d.Settle();
        var other = Assert.Single(d.AppState.Tabs);

        Assert.Equal("Greeter", first.Node!.Text);
        Assert.Equal([firstPanel], firstDocuments);
        Assert.Equal("Farewell", other.Node!.Text);
        Assert.Equal([d.Ui.Documents.PanelOf(other)], d.Ui.MainLayout.ListDocuments());
    }

    [Fact]
    public void Switching_back_to_a_list_docks_its_documents_side_by_side_as_they_were()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        KeepTabOpen(d.AppState, d.AppState.Tabs.Active!);
        var greeterPanel = d.Ui.Documents.PanelOf(d.AppState.Tabs.Active!);
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        var layout = d.Ui.Layout;
        layout.Dock(d.Ui.Documents.PanelOf(d.AppState.Tabs.Active!), layout.GroupOf(greeterPanel)!, DockEdge.Right);
        d.Settle();
        d.AppState.Lists.Create(Other);
        SwitchAssemblyList(d.AppState, Other);
        d.Settle();

        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        d.Settle();

        var tabs = d.AppState.Tabs.ToList();
        Assert.Equal(["Greeter", "Farewell"], tabs.Select(static t => t.Title));
        var left = layout.GroupOf(d.Ui.Documents.PanelOf(tabs[0]))!;
        var right = layout.GroupOf(d.Ui.Documents.PanelOf(tabs[1]))!;
        var split = ParentOf(layout.Root, left)!;
        Assert.Equal(Direction.Horizontal, split.Direction);
        Assert.Same(left, split.First);
        Assert.Same(right, split.Second);
        Assert.Equal("Farewell", d.AppState.Tabs.Active!.Title);
    }

    private static DockSplit? ParentOf(DockNode node, DockNode target) => node switch
    {
        DockSplit split when split.First == target || split.Second == target => split,
        DockSplit split => ParentOf(split.First, target) ?? ParentOf(split.Second, target),
        _ => null,
    };

    [Fact]
    public void A_hand_edited_list_keeps_one_document_per_panel_and_none_docked_as_a_view()
    {
        var settings = _temp.Settings();
        List<DocumentRecord> tabs = [HandTab("document-1", "First"), HandTab("document-1", "Twin"), HandTab(DockPanels.Assemblies, "Tree")];
        settings.AssemblyLists.Add(new AssemblyListRecord("Hand", [], Documents: new DocumentsRecord(tabs, null, null, null)));
        settings.Save();

        var kept = _temp.Settings().AssemblyLists.Single(static l => l.Name == "Hand").Documents!.Tabs;

        Assert.Equal(["First"], kept.Select(static t => t.Title));
    }

    private static NodeDocumentRecord HandTab(string panel, string title) =>
        new(panel, title, NodePath.From(PathOf(AppStateDriver.FixturePath), []), null, DecompilationLanguage.CSharp, null);

    [Fact]
    public void A_new_panel_skips_every_panel_saved_documents_were_docked_as_however_numbered()
    {
        var documents = new DocumentViews();

        documents.SkipPanels(["document-1", "document-2", "document-2147483647"]);

        Assert.Equal("document-3", documents.NewPanel());
    }

    [Fact]
    public void Tabs_showing_nothing_are_not_kept_and_the_next_run_starts_with_one_empty_tab_as_ever()
    {
        var d = new AppStateDriver(_temp.Settings());
        d.NextFrame();
        Settings.SaveSettings(d.AppState);

        var saved = _temp.Settings();
        var next = new AppStateDriver(saved);

        Assert.Null(Assert.Single(saved.AssemblyLists).Documents);
        var tab = Assert.Single(next.AppState.Tabs);
        Assert.Null(tab.Location);
        Assert.True(next.Ui.Layout.Contains(next.Ui.Documents.PanelOf(tab)));
    }

    [Fact]
    public void A_tab_opened_is_saved_with_the_next_frame()
    {
        var app = new AppDriver(settings: _temp.Settings());
        app.Driver.OpenFixtures();
        app.Run(2);

        app.Show(app.Fixture("Greeter"), 1, inNewTab: true);

        var documents = Assert.Single(_temp.Settings().AssemblyLists).Documents!;
        Assert.Contains(documents.Tabs, static t => t.Title == "Greeter");
    }
}
