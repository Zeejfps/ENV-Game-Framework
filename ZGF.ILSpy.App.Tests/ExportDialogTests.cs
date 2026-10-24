using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportDialogTests : IDisposable
{
    private static readonly string SecondPath = typeof(KeyboardKey).Assembly.Location;

    private readonly TempDir _temp = new();

    public ExportDialogTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    private static AppStateDriver OpenedTwo()
    {
        var d = AppStateDriver.Opened();
        d.AppState.Open(SecondPath);
        d.RunUntil(() => d.AppState.Assemblies.Assemblies.Count == 2);
        d.Settle();
        return d;
    }

    private static LoadedAssembly Fixtures(AppStateDriver d) => d.Node().Assembly;

    [Fact]
    public void The_dialog_opens_with_the_picked_assembly_ticked()
    {
        var d = OpenedTwo();

        d.Ui.OpenExportSetup(Fixtures(d));

        var setup = d.Ui.ExportSetup;
        Assert.True(setup.IsOpen);
        Assert.Equal([true, false], setup.Choices.Select(c => c.Included));
        Assert.False(setup.IsSolution);
    }

    [Fact]
    public void The_only_open_assembly_is_ticked_without_being_picked()
    {
        var d = AppStateDriver.Opened();

        d.Ui.OpenExportSetup(null);

        Assert.True(Assert.Single(d.Ui.ExportSetup.Choices).Included);
    }

    [Fact]
    public void Ticking_a_second_assembly_makes_the_export_a_solution_named_after_the_folder()
    {
        var d = OpenedTwo();
        d.Ui.OpenExportSetup(Fixtures(d));
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.PathOf("Out");

        setup.Choices[1].Included = true;

        Assert.True(setup.IsSolution);
        Assert.Equal("Out.sln", setup.SolutionFileName);
        setup.SolutionName = "Mine";
        Assert.Equal("Mine.sln", setup.SolutionFileName);
    }

    [Fact]
    public void Two_projects_with_one_name_stop_the_export_until_one_is_renamed()
    {
        var d = OpenedTwo();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;
        setup.IncludeAll(true);

        setup.Choices[1].Name = setup.Choices[0].Name.ToUpperInvariant();

        Assert.NotNull(setup.Clash);
        Assert.False(setup.CanExport);
        setup.Choices[1].Name = "Other";
        Assert.Null(setup.Clash);
        Assert.True(setup.CanExport);
    }

    [Fact]
    public void The_export_needs_a_full_folder_path_and_a_ticked_assembly()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;

        setup.Folder = "relative";
        Assert.False(setup.CanExport);
        setup.Folder = _temp.Path;
        Assert.True(setup.CanExport);
        setup.IncludeAll(false);
        Assert.False(setup.CanExport);
    }

    [Fact]
    public void What_stops_the_export_is_named_one_thing_at_a_time()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        var choice = Assert.Single(setup.Choices);

        setup.IncludeAll(false);
        Assert.Equal(ExportBlocker.NoAssembly, setup.Blocker);
        setup.IncludeAll(true);
        Assert.Equal(ExportBlocker.NoFolder, setup.Blocker);
        setup.Folder = _temp.Path;
        choice.Name = " ";
        Assert.Equal(ExportBlocker.NamelessProject, setup.Blocker);
        choice.Name = "Project";
        Assert.Equal(ExportBlocker.None, setup.Blocker);
    }

    [Fact]
    public void A_folder_that_holds_files_is_pointed_out()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;
        Assert.False(setup.FolderHoldsFiles);

        setup.Folder = _temp.PathOf("Full");
        _temp.Write(Path.Combine("Full", "notes.txt"), "keep");

        Assert.True(setup.FolderHoldsFiles);
    }

    [Fact]
    public void One_ticked_assembly_exports_as_a_project_straight_into_the_folder()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;
        setup.Choices[0].Name = "Renamed";

        d.Ui.ExportSetup.Start(d.AppState);
        d.Settle();

        Assert.False(setup.IsOpen);
        Assert.True(File.Exists(_temp.PathOf("Renamed.csproj")));
    }

    [Fact]
    public void Several_ticked_assemblies_export_as_a_solution_with_a_folder_per_project()
    {
        var d = OpenedTwo();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;
        setup.IncludeAll(true);
        setup.SolutionName = "Both";

        d.Ui.ExportSetup.Start(d.AppState);
        d.Settle();

        Assert.True(File.Exists(_temp.PathOf("Both.sln")));
        foreach (var choice in setup.Choices)
            Assert.True(File.Exists(_temp.PathOf(Path.Combine(choice.Name, choice.Name + ".csproj"))));
    }

    [Fact]
    public void With_nothing_ticked_the_dialog_opens_on_the_Choose_page()
    {
        var d = OpenedTwo();

        d.Ui.OpenExportSetup(null);
        Assert.Equal(ExportPage.Choose, d.Ui.ExportSetup.Page);

        d.Ui.OpenExportSetup(Fixtures(d));
        Assert.Equal(ExportPage.Main, d.Ui.ExportSetup.Page);
    }

    [Fact]
    public void The_Choose_page_lists_ticked_assemblies_first_and_keeps_its_order_while_ticking()
    {
        var d = OpenedTwo();
        var second = d.AppState.Assemblies.Assemblies[1];
        d.Ui.OpenExportSetup(second);
        var setup = d.Ui.ExportSetup;

        setup.Choose();
        var first = setup.Picker.Shown();
        first[1].Included = true;

        Assert.Equal(second, first[0].Assembly);
        Assert.Equal(first, setup.Picker.Shown());
    }

    [Fact]
    public void The_filter_narrows_the_list_and_Select_all_shown_ticks_only_what_it_shows()
    {
        var d = OpenedTwo();
        d.Ui.OpenExportSetup(null);
        var picker = d.Ui.ExportSetup.Picker;
        var second = d.AppState.Assemblies.Assemblies[1];

        picker.Filter = second.Name.ToUpperInvariant();
        var shown = Assert.Single(picker.Shown());
        picker.IncludeShown(true);

        Assert.Equal(second, shown.Assembly);
        Assert.Equal([false, true], d.Ui.ExportSetup.Choices.Select(c => c.Included));
        Assert.Equal(true, picker.AllShownIncluded);
    }

    [Fact]
    public void Selected_only_hides_the_unticked_assemblies()
    {
        var d = OpenedTwo();
        d.Ui.OpenExportSetup(Fixtures(d));
        var setup = d.Ui.ExportSetup;
        setup.Choose();

        setup.Picker.SelectedOnly = true;

        Assert.Equal(Fixtures(d), Assert.Single(setup.Picker.Shown()).Assembly);
    }

    [Fact]
    public void The_main_page_shows_only_the_ticked_assemblies()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.AppState.Open(SecondPath);
        app.Driver.RunUntil(() => app.AppState.Assemblies.Assemblies.Count == 2);
        var second = app.AppState.Assemblies.Assemblies[1];
        app.Run(2);
        var drawnByTheTree = app.Frames.Canvas.Texts.Count(t => t.Inputs.Text == second.Name);

        app.Ui.OpenExportSetup(app.AppState.Assemblies.Assemblies[0]);
        app.Run(2);

        Assert.Equal(drawnByTheTree, app.Frames.Canvas.Texts.Count(t => t.Inputs.Text == second.Name));
        Assert.False(app.Frames.Drew("Select all shown"));
    }

    [Fact]
    public void Choose_opens_the_list_and_Done_or_Escape_goes_back_without_closing()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.Ui.OpenExportSetup(null);
        app.Run(2);
        var setup = app.Ui.ExportSetup;

        app.Click(LucideIcons.Plus).Frame(2);
        Assert.Equal(ExportPage.Choose, setup.Page);
        app.Click("Done").Frame(2);
        Assert.Equal(ExportPage.Main, setup.Page);

        app.Click(LucideIcons.Plus).Frame(2);
        app.Frames.Key(KeyboardKey.Escape).Frame(2);

        Assert.Equal(ExportPage.Main, setup.Page);
        Assert.True(setup.IsOpen);
    }

    [Fact]
    public void A_filter_that_hides_everything_says_so_and_can_be_cleared()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.Ui.OpenExportSetup(null);
        var setup = app.Ui.ExportSetup;
        setup.Choose();
        setup.Picker.Filter = "nothing-is-called-this";
        app.Run(2);

        Assert.True(app.Frames.Drew("No assemblies match the filter."));
        app.Click("Clear filter").Frame(2);

        Assert.Equal("", setup.Picker.Filter);
        Assert.Single(setup.Picker.Shown());
    }

    [Fact]
    public void With_nothing_chosen_the_main_page_offers_to_choose()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.Ui.OpenExportSetup(null);
        app.Ui.ExportSetup.IncludeAll(false);
        app.Run(2);

        app.Click(LucideIcons.Plus).Frame(2);

        Assert.Equal(ExportPage.Choose, app.Ui.ExportSetup.Page);
    }

    private static AppDriver WithTwoIncluded()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.AppState.Open(SecondPath);
        app.Driver.RunUntil(() => app.AppState.Assemblies.Assemblies.Count == 2);
        app.Ui.OpenExportSetup(null);
        app.Ui.ExportSetup.IncludeAll(true);
        app.Ui.ExportSetup.FinishChoosing();
        app.Run(2);
        return app;
    }

    /// <summary>Clicks the assembly cell of <paramref name="assembly"/>'s row: the first of the texts
    /// on the row of the last one drawn, which is its project name.</summary>
    private static void ClickRowOf(AppDriver app, LoadedAssembly assembly, KeyModifiers modifiers = KeyModifiers.None)
    {
        var projectName = app.LastTextRect(assembly.Name);
        var cell = app.Frames.TextRects()
            .Where(t => t.Text == assembly.Name && t.Rect.Y == projectName.Y)
            .MinBy(t => t.Rect.X)
            .Rect;
        app.Frames.Click(cell.X + 2f, cell.Y + cell.Height / 2, modifiers: modifiers).Frame();
    }

    [Fact]
    public void Minus_removes_the_picked_out_rows_and_picks_out_the_next()
    {
        var app = WithTwoIncluded();
        var setup = app.Ui.ExportSetup;
        var first = app.AppState.Assemblies.Assemblies[0];
        var second = app.AppState.Assemblies.Assemblies[1];
        Assert.False(setup.RowSelection.Count > 0);

        ClickRowOf(app, first);
        app.Click(LucideIcons.Minus).Frame(2);

        Assert.Equal([false, true], setup.Choices.Select(c => c.Included));
        Assert.True(setup.RowSelection.IsOnly(setup.Choices[1]));
        Assert.Equal(second, setup.IncludedChoices()[0].Assembly);
    }

    [Fact]
    public void Delete_removes_every_row_picked_out_with_ctrl()
    {
        var app = WithTwoIncluded();
        var setup = app.Ui.ExportSetup;

        ClickRowOf(app, app.AppState.Assemblies.Assemblies[0]);
        ClickRowOf(app, app.AppState.Assemblies.Assemblies[1], KeyModifiers.Control);
        Assert.Equal(2, setup.RowSelection.Count);
        app.Frames.Key(KeyboardKey.Delete).Frame(2);

        Assert.Equal(0, setup.IncludedCount);
        Assert.True(app.Frames.Drew("No assemblies included yet."));
    }

    [Fact]
    public void Ctrl_Shift_E_opens_the_dialog_and_Export_starts_it()
    {
        var app = AppDriver.Opened();
        app.Run(2);

        app.Frames.Key(KeyboardKey.E, KeyModifiers.Control | KeyModifiers.Shift).Frame(2);
        Assert.True(app.Ui.ExportSetup.IsOpen);
        app.Frames.FilePicker.Answer = _temp.Path;
        app.Click("Browse…").Frame(2);
        Assert.Equal(_temp.Path, app.Ui.ExportSetup.Folder);

        app.ClickMenuItem("Export").Frame(2);
        app.Settle(1);

        Assert.False(app.Ui.ExportSetup.IsOpen);
        Assert.Single(app.AppState.FinishedExports);
    }

    [Fact]
    public void Export_on_an_assembly_in_the_tree_opens_the_dialog_with_that_assembly_ticked()
    {
        var app = AppDriver.Opened();
        app.AppState.Open(SecondPath);
        app.Driver.RunUntil(() => app.AppState.Assemblies.Assemblies.Count == 2);
        app.Settle(2);
        var second = app.AppState.Assemblies.Assemblies[1];
        var row = app.TextRect(app.Ui.Tree.Label(second.Root));

        app.Frames.Click(row.X + 2f, row.Y + row.Height / 2, PointerButton.Right).Frame();
        app.ClickMenuItem("Export…").Frame(2);

        var setup = app.Ui.ExportSetup;
        Assert.True(setup.IsOpen);
        Assert.Equal([false, true], setup.Choices.Select(c => c.Included));
    }

    [Fact]
    public void The_export_options_live_in_the_dialog_and_not_in_the_decompiler_options()
    {
        var app = AppDriver.Opened(height: 1400f);
        app.Ui.OpenDecompilerOptions();
        app.Run(2);
        Assert.False(app.Frames.Drew("SDK-style project files"));

        app.Ui.OpenExportSetup(null);
        app.Run(2);

        Assert.True(app.Frames.Drew("SDK-style project files"));
        Assert.True(app.Frames.Drew("These also change the code you browse."));
    }

    [Fact]
    public void A_project_option_ticked_in_the_dialog_is_kept_for_the_next_export()
    {
        var app = AppDriver.Opened(height: 1400f);
        var nested = DecompilerToggle.Find("UseNestedDirectoriesForNamespaces")!;
        app.Ui.OpenExportSetup(null);
        app.Run(2);

        app.Click("A folder per namespace part").Frame(2);

        Assert.Equal(!nested.DefaultValue, app.AppState.DecompilerOptions.IsOn(nested));
    }

    [Fact]
    public void A_key_file_that_is_not_there_stops_the_export()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;

        setup.StrongNameKeyFile = _temp.PathOf("missing.snk");
        Assert.True(setup.KeyFileMissing);
        Assert.False(setup.CanExport);

        setup.StrongNameKeyFile = _temp.Write("signing.snk", [1, 2, 3]);
        Assert.False(setup.KeyFileMissing);
        Assert.True(setup.CanExport);
    }

    [Fact]
    public void A_pdb_asked_for_in_the_dialog_is_written_beside_the_project_and_reported()
    {
        var d = AppStateDriver.Opened();
        d.Ui.OpenExportSetup(null);
        var setup = d.Ui.ExportSetup;
        setup.Folder = _temp.Path;
        setup.GeneratePdb = true;

        d.Ui.ExportSetup.Start(d.AppState);
        d.Settle();

        var pdb = _temp.PathOf(Fixtures(d).PdbFileName);
        Assert.True(File.Exists(pdb));
        Assert.Contains(pdb, Assert.Single(d.AppState.FinishedExports).Written);
    }

    [Fact]
    public void Resetting_the_decompiler_options_keeps_how_projects_are_exported()
    {
        var d = AppStateDriver.Opened();
        var sdk = DecompilerToggle.Find("UseSdkStyleProjectFormat")!;
        d.AppState.SetDecompilerToggle(sdk, !sdk.DefaultValue);

        d.AppState.ResetDecompilerToggles();

        Assert.Equal(!sdk.DefaultValue, d.AppState.DecompilerOptions.IsOn(sdk));
    }

    [Fact]
    public void The_dialog_warns_in_place_about_a_folder_that_holds_files()
    {
        var app = AppDriver.Opened();
        _temp.Write("notes.txt", "keep");
        app.Ui.OpenExportSetup(null);
        app.Ui.ExportSetup.Folder = _temp.Path;

        app.Run(2);

        Assert.Contains(app.Frames.Canvas.Texts, t => t.Inputs.Text.StartsWith("This folder already holds files.", StringComparison.Ordinal));
    }
}
