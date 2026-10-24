using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.AssemblyLists;
using static ZGF.ILSpy.Docking;
using static ZGF.ILSpy.Settings;

namespace ZGF.ILSpy.App.Tests;

public sealed class SavedAssemblyListsTests
{
    [Fact]
    public void A_new_list_is_numbered_when_its_name_is_taken()
    {
        var lists = new SavedAssemblyLists();

        var first = lists.Create("New list");
        var second = lists.Create("new list");

        Assert.Equal("New list", first);
        Assert.Equal("new list 2", second);
        Assert.Equal([SavedAssemblyLists.DefaultName, "New list", "new list 2"], lists.Names);
    }

    [Fact]
    public void A_list_is_never_made_without_a_name()
    {
        var lists = new SavedAssemblyLists();

        Assert.Throws<ArgumentException>(() => lists.Create(" "));
        Assert.Throws<ArgumentException>(() => lists.Duplicate(SavedAssemblyLists.DefaultName, ""));
        Assert.Equal([SavedAssemblyLists.DefaultName], lists.Names);
    }

    [Fact]
    public void A_duplicate_holds_the_same_assemblies_under_a_name_of_its_own()
    {
        var lists = new SavedAssemblyLists();
        lists.RememberActive(["a.dll", "b.dll"]);

        var copy = lists.Duplicate(SavedAssemblyLists.DefaultName, "Default copy");
        lists.RememberActive(["c.dll"]);

        Assert.Equal("Default copy", copy);
        Assert.Equal(["a.dll", "b.dll"], lists.PathsOf(copy));
    }

    [Fact]
    public void A_list_is_not_renamed_to_nothing_or_to_another_lists_name()
    {
        var lists = new SavedAssemblyLists();
        lists.Create("Work");

        Assert.False(lists.Rename("Work", "  "));
        Assert.False(lists.Rename("Work", "default"));
        Assert.True(lists.Rename("Work", "WORK"));
        Assert.True(lists.Rename(SavedAssemblyLists.DefaultName, " Play "));
        Assert.Equal(["Play", "WORK"], lists.Names);
        Assert.Equal("Play", lists.Active);
    }

    [Fact]
    public void Deleting_the_open_list_opens_the_one_that_takes_its_place_and_the_last_leaves_an_empty_default()
    {
        var lists = new SavedAssemblyLists();
        lists.Create("Work");
        lists.Create("Play");
        lists.Activate("Play");

        lists.Delete("Play");
        Assert.Equal("Work", lists.Active);

        lists.Delete("Work");
        lists.RememberActive(["a.dll"]);
        lists.Delete(SavedAssemblyLists.DefaultName);

        Assert.Equal([SavedAssemblyLists.DefaultName], lists.Names);
        Assert.Empty(lists.ActivePaths);
    }

    [Fact]
    public void Loading_drops_nameless_and_repeated_lists_and_opens_the_named_one_or_else_the_first()
    {
        var lists = new SavedAssemblyLists();
        List<AssemblyListRecord> records =
        [
            new(" ", ["x.dll"]),
            new("Work", ["a.dll"]),
            new("work", ["b.dll"]),
            new("Play", ["c.dll"]),
        ];

        lists.Load(records, "play");
        Assert.Equal(["Work", "Play"], lists.Names);
        Assert.Equal("Play", lists.Active);

        lists.Load(records, "Gone");
        Assert.Equal("Work", lists.Active);
        Assert.Equal(["a.dll"], lists.ActivePaths);
    }
}

public sealed class AssemblyListingTests
{
    private const string Other = "Other";

    [Fact]
    public void The_open_assemblies_are_saved_as_the_open_lists()
    {
        using var temp = new TempDir();
        var d = AppStateDriver.Opened(temp.Settings());
        SaveSettings(d.AppState);

        var loaded = temp.Settings();

        Assert.Equal(SavedAssemblyLists.DefaultName, loaded.ActiveAssemblyList);
        Assert.Equal([AppStateDriver.FixturePath], Assert.Single(loaded.AssemblyLists).Assemblies);
    }

    [Fact]
    public void The_list_restored_at_startup_is_saved_whole_while_its_assemblies_are_still_opening()
    {
        using var temp = new TempDir();
        var other = typeof(AppState).Assembly.Location;
        var settings = temp.Settings();
        settings.AssemblyLists = [new AssemblyListRecord(SavedAssemblyLists.DefaultName, [AppStateDriver.FixturePath, other])];
        var d = new AppStateDriver(settings);

        RestoreActiveAssemblyList(d.AppState);
        SaveSettings(d.AppState);

        Assert.Equal([AppStateDriver.FixturePath, other], Assert.Single(temp.Settings().AssemblyLists).Assemblies);
        d.Settle();
        Assert.Equal([AppStateDriver.FixturePath, other], d.AppState.Assemblies.FilePaths);
    }

    [Fact]
    public void Switching_lists_keeps_the_analyses_of_the_last_run_still_being_found_with_their_list()
    {
        using var temp = new TempDir();
        var first = AppStateDriver.Opened(temp.Settings());
        first.Ui.Analyze(first.Fixture("Greeter"));
        SaveSettings(first.AppState);
        var d = new AppStateDriver(temp.Settings());
        d.AppState.Lists.Create(Other);

        SwitchAssemblyList(d.AppState, Other);
        d.Settle();
        SaveSettings(d.AppState);

        var lists = temp.Settings().AssemblyLists;
        Assert.Empty(d.AppState.Analyzer.Roots);
        Assert.Single(lists[0].Analyzed!);
        Assert.Empty(lists[1].Analyzed!);
    }

    [Fact]
    public void Switching_back_to_a_list_analyzes_again_what_was_analyzed_in_it()
    {
        var d = AppStateDriver.Opened();
        d.Ui.Analyze(d.Fixture("Greeter"));
        d.AppState.Lists.Create(Other);

        SwitchAssemblyList(d.AppState, Other);
        d.Settle();
        var inOther = d.AppState.Analyzer.Roots.Count;
        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        d.Settle();

        Assert.Equal(0, inOther);
        Assert.Contains("Greeter", Assert.Single(d.AppState.Analyzer.Found()).Symbol.Label);
    }

    [Fact]
    public void A_duplicated_list_has_the_analyses_of_the_one_it_copies()
    {
        var d = AppStateDriver.Opened();
        d.Ui.Analyze(d.Fixture("Greeter"));
        var copy = DuplicateAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);

        SwitchAssemblyList(d.AppState, copy);
        d.Settle();

        Assert.NotEqual(SavedAssemblyLists.DefaultName, copy);
        Assert.Equal(copy, d.AppState.Lists.Active);
        Assert.Contains("Greeter", Assert.Single(d.AppState.Analyzer.Found()).Symbol.Label);
    }

    [Fact]
    public void Switching_lists_closes_the_assemblies_documents_and_history_of_the_one_before_and_switching_back_reopens_them()
    {
        var d = AppStateDriver.Opened();
        d.Show(d.Fixture("Greeter"));
        d.Show(d.Fixture("Farewell"), inNewTab: true);
        d.AppState.Lists.Create(Other);

        SwitchAssemblyList(d.AppState, Other);
        d.Settle();

        Assert.Empty(d.AppState.Assemblies.Assemblies);
        Assert.Null(Assert.Single(d.AppState.Tabs).Location);
        Assert.Empty(d.AppState.History.Entries);

        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        d.Settle();

        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Assemblies.FilePaths);
    }

    [Fact]
    public void Switching_lists_with_every_document_closed_opens_an_empty_one()
    {
        var d = AppStateDriver.Opened();
        d.AppState.Lists.Create(Other);
        foreach (var tab in d.AppState.Tabs.ToList())
            CloseTab(d.AppState, tab);

        SwitchAssemblyList(d.AppState, Other);
        d.Settle();

        var empty = Assert.Single(d.AppState.Tabs);
        Assert.Same(empty, d.AppState.Tabs.Active);
        Assert.True(d.Ui.Layout.Contains(d.Ui.PanelOf(empty)));
        Assert.Null(empty.Location);
    }

    [Fact]
    public void A_new_list_opens_empty_and_what_is_opened_in_it_stays_out_of_the_list_before()
    {
        var d = AppStateDriver.Opened();
        var other = typeof(AppState).Assembly.Location;

        CreateAssemblyList(d.AppState);
        SwitchAssemblyList(d.AppState, "New list");
        d.Settle();
        OpenAssembly(d.AppState, other);
        d.RunUntil(() => d.AppState.Assemblies.FilePaths.Contains(other));
        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        d.Settle();

        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Assemblies.FilePaths);
        Assert.Equal([other], d.AppState.Lists.PathsOf("New list"));
    }

    [Fact]
    public void An_assembly_still_opening_when_the_list_is_switched_stays_out_of_the_next_list()
    {
        var d = AppStateDriver.Opened();
        d.AppState.Lists.Create(Other);

        OpenAssembly(d.AppState, typeof(AppState).Assembly.Location);
        SwitchAssemblyList(d.AppState, Other);
        d.Settle();
        d.Pump();

        Assert.Empty(d.AppState.Assemblies.Assemblies);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Switching_lists_stops_the_assemblies_still_opening_from_landing_in_the_next_list()
    {
        var d = AppStateDriver.Opened();
        d.AppState.Lists.Create(Other);
        var opening = d.AppState.Opens.Token;

        SwitchAssemblyList(d.AppState, Other);

        Assert.True(opening.IsCancellationRequested);
        Assert.False(d.AppState.Opens.Token.IsCancellationRequested);
    }

    [Fact]
    public void Deleting_the_open_list_opens_the_assemblies_of_the_one_that_takes_its_place()
    {
        var d = AppStateDriver.Opened();
        CreateAssemblyList(d.AppState);
        d.Settle();

        DeleteAssemblyList(d.AppState, "New list");
        d.Settle();

        Assert.Equal(SavedAssemblyLists.DefaultName, d.AppState.Lists.Active);
        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Assemblies.FilePaths);
    }

    [Fact]
    public void Reloading_reopens_the_whole_list_even_while_it_is_still_being_opened()
    {
        var d = AppStateDriver.Opened();
        d.AppState.Lists.Create(Other);
        SwitchAssemblyList(d.AppState, Other);
        d.Settle();

        SwitchAssemblyList(d.AppState, SavedAssemblyLists.DefaultName);
        RefreshAssemblies(d.AppState, d.Ui.Tree.ExpandedPaths());
        d.Settle();

        Assert.Equal([AppStateDriver.FixturePath], d.AppState.Assemblies.FilePaths);
    }

    [Fact]
    public void Expanding_the_tree_expands_the_assemblies_and_their_namespaces()
    {
        var d = AppStateDriver.Opened();
        var tree = d.Ui.Tree;
        var root = tree.Roots[0];
        tree.CollapseAll();

        tree.ExpandNamespaces();
        var ns = d.Node(AppStateDriver.FixturesNamespace);
        d.RunUntil(() => tree.View.IsExpanded(ns));

        Assert.True(tree.View.IsExpanded(root));
        Assert.False(tree.View.IsExpanded(d.Fixture("Greeter")));
    }

    [Fact]
    public void The_assemblies_panels_remove_button_closes_the_selected_assembly_only()
    {
        var app = AppDriver.Opened();
        var root = app.Ui.Tree.Roots[0];
        app.Ui.Tree.View.Select(app.Node(AppStateDriver.FixturesNamespace));
        app.Run(2);

        app.Click(LucideIcons.Trash2);
        Assert.Single(app.AppState.Assemblies.Assemblies);

        app.Ui.Tree.View.Select(root);
        app.Run(2);
        app.Click(LucideIcons.Trash2);

        Assert.Empty(app.AppState.Assemblies.Assemblies);
    }

    [Fact]
    public void The_assemblies_panel_names_the_open_list_and_the_dialog_counts_each_lists_assemblies()
    {
        var app = AppDriver.Opened();
        app.AppState.Lists.Create(Other);
        app.Run(2);
        Assert.True(app.Frames.Drew(SavedAssemblyLists.DefaultName));

        app.Ui.ListsDialog.Open(app.AppState);
        app.Run(2);

        Assert.True(app.Frames.Drew("Assembly lists"));
        Assert.True(app.Frames.Drew("1 assembly"));
        Assert.True(app.Frames.Drew("0 assemblies"));
        Assert.True(app.Frames.Drew(LucideIcons.Check));
    }

    [Fact]
    public void A_rows_activate_button_switches_to_its_list_and_leaves_the_dialog_open()
    {
        var app = OpenedDialog();

        app.ClickLast("Activate").Frame(2);
        app.Settle(2);

        Assert.Equal(Other, app.AppState.Lists.Active);
        Assert.True(app.Ui.ListsDialog.IsOpen);
        Assert.Empty(app.AppState.Assemblies.Assemblies);
    }

    [Fact]
    public void A_new_list_is_named_in_place_without_switching_to_it()
    {
        var app = OpenedDialog();

        app.Click("New list").Frame(2);
        app.Frames.Type("Work").Frame(2);
        app.Frames.Key(KeyboardKey.Enter).Frame(2);

        Assert.Contains("Work", app.AppState.Lists.Names);
        Assert.Equal(SavedAssemblyLists.DefaultName, app.AppState.Lists.Active);
        Assert.Null(app.Ui.ListsDialog.Renaming);
    }

    [Fact]
    public void Double_clicking_a_list_renames_it_and_escape_drops_the_name_but_keeps_the_dialog_open()
    {
        var app = OpenedDialog();

        app.DoubleClick(Other).Frame(2);
        Assert.Equal(Other, app.Ui.ListsDialog.Renaming);
        app.Frames.Type("Gone").Frame(2);
        app.Frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.Equal([SavedAssemblyLists.DefaultName, Other], app.AppState.Lists.Names);
        Assert.Null(app.Ui.ListsDialog.Renaming);
        Assert.True(app.Ui.ListsDialog.IsOpen);
        Assert.Equal(SavedAssemblyLists.DefaultName, app.AppState.Lists.Active);
    }

    [Fact]
    public void Deleting_the_active_list_says_which_opens_instead_once_confirmed()
    {
        var app = OpenedDialog();

        app.ClickLast(LucideIcons.Trash2).Frame(2);
        Assert.True(app.Frames.Drew("Delete assembly list"));
        Assert.Equal(SavedAssemblyLists.DefaultName, app.Ui.ListsDialog.Deleting);
        Assert.Equal(Other, app.AppState.Lists.ReplacementFor(SavedAssemblyLists.DefaultName));
        app.Click("Delete list").Frame(2);
        app.Settle(2);

        Assert.Equal([Other], app.AppState.Lists.Names);
        Assert.Equal(Other, app.AppState.Lists.Active);
    }

    [Fact]
    public void Escape_closes_the_delete_confirmation_and_leaves_the_lists_dialog_open()
    {
        var app = OpenedDialog();

        app.ClickLast(LucideIcons.Trash2).Frame(2);
        app.Frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.False(app.Frames.Drew("Delete assembly list"));
        Assert.Null(app.Ui.ListsDialog.Deleting);
        Assert.True(app.Ui.ListsDialog.IsOpen);
        Assert.Equal([SavedAssemblyLists.DefaultName, Other], app.AppState.Lists.Names);
    }

    /// <summary>An app with the fixtures open in the Default list, beside an empty one, and the dialog
    /// open on Default.</summary>
    private static AppDriver OpenedDialog()
    {
        var app = AppDriver.Opened();
        app.AppState.Lists.Create(Other);
        app.Ui.ListsDialog.Open(app.AppState);
        app.Run(2);
        return app;
    }
}
