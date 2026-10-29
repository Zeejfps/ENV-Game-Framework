using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using ZGF.KeyboardModule;
using static ZGF.ILSpy.Assemblies;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.App.Tests.BuiltInGlyphs;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportTests : IDisposable
{
    private static readonly string SampleAppPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.dll");

    private readonly TempDir _temp = new();

    public ExportTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    /// <summary>A driver that has opened the small sample app, for what works through a whole assembly,
    /// which would take long on the fixtures, the whole test assembly.</summary>
    private static AppStateDriver OpenedSample()
    {
        var d = new AppStateDriver();
        OpenAssembly(d.AppState, SampleAppPath);
        d.RunUntil(() => d.AppState.IsListed(SampleAppPath));
        d.Settle();
        return d;
    }

    private static LoadedModule SampleOf(AppStateDriver d) => Assert.Single(d.AppState.Assemblies.Assemblies);

    [Fact]
    public void Saving_code_writes_the_node_in_the_current_language()
    {
        var d = AppStateDriver.Opened();
        SetLanguage(d.AppState, DecompilationLanguage.IL);
        var path = _temp.PathOf("Greeter.il");

        SaveCode(d.AppState, d.Fixture("Greeter"), path);
        d.Settle();

        Assert.Contains(".class public auto ansi beforefieldinit ZGF.ILSpy.App.Tests.Fixtures.Greeter", File.ReadAllText(path));
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void Saving_an_assembly_writes_one_code_file_even_under_a_project_name()
    {
        var d = OpenedSample();
        var path = _temp.PathOf("SampleApp.csproj");

        SaveCode(d.AppState, SampleOf(d).Root, path);
        d.Settle();

        var code = File.ReadAllText(path);
        Assert.Contains("public class Counter", code);
        Assert.False(code.TrimStart().StartsWith("<Project", StringComparison.Ordinal));
        Assert.Equal([path], Directory.EnumerateFiles(_temp.Path));
    }

    [Fact]
    public void Saving_a_resource_writes_its_bytes_as_they_are()
    {
        var d = AppStateDriver.Opened();
        var node = d.Node("Resources", "data.json");
        var path = _temp.PathOf("data.json");

        Assert.True(CanSaveCode(d.AppState, node));
        SaveCode(d.AppState, node, path);

        Assert.Equal(node.ReadResource(), File.ReadAllBytes(path));
    }

    [Fact]
    public void Pdbs_are_generated_for_every_open_assembly_into_the_folder()
    {
        var d = OpenedSample();
        var assembly = SampleOf(d);

        GeneratePdbs(d.AppState, _temp.Path);
        d.Settle();

        Assert.True(File.Exists(_temp.PathOf(assembly.PdbFileName)));
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void The_tree_menu_creates_a_diagram_in_a_folder_named_after_the_assembly_and_opens_it()
    {
        var app = new AppDriver();
        OpenAssembly(app.AppState, SampleAppPath);
        app.RunUntil(() => app.AppState.IsListed(SampleAppPath));
        app.Settle(2);
        app.Frames.FilePicker.Answer = _temp.Path;

        app.PickFromTreeMenu(SampleOf(app.Driver).Name, "Create diagram…");
        app.RunUntil(() => app.Frames.OpenedFiles.Count == 1);

        var page = Path.Combine(_temp.Path, "SampleApp diagram", "index.html");
        var report = Assert.Single(app.AppState.FinishedExports);
        Assert.Equal(["Choose the folder for the diagram"], app.Frames.FilePicker.Titles);
        Assert.Equal(ExportKind.Diagram, report.Job.Kind);
        Assert.Equal([page], report.Written);
        Assert.Equal([page], app.Frames.OpenedFiles);
        Assert.Contains("class Counter", File.ReadAllText(page));
        Assert.Empty(app.AppState.Errors.Entries);
    }

    [Fact]
    public void Ctrl_S_saves_the_active_tab_where_the_user_picks()
    {
        var app = AppDriver.Opened();
        app.Show(app.Fixture("Greeter"), frames: 2);
        var path = _temp.PathOf("Greeter.cs");
        app.Frames.FilePicker.Answer = path;

        app.Frames.Key(Shortcuts.SaveCode).Frame(2);
        app.Settle();

        Assert.Equal(["Save code"], app.Frames.FilePicker.Titles);
        Assert.Contains("public class Greeter", File.ReadAllText(path));
    }

    [Fact]
    public void Ctrl_S_on_baml_offers_to_save_it_as_xaml()
    {
        var app = AppDriver.Opened();
        var sample = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.dll");
        OpenAssembly(app.AppState, sample);
        app.Driver.RunUntil(() => app.AppState.Assemblies.Assemblies.Any(a => a.FilePath == sample));
        var assembly = app.AppState.Assemblies.Assemblies.Single(a => a.FilePath == sample);
        var baml = assembly.Root.Child("Resources", "SampleApp.g.resources", "mainwindow.baml");
        app.Show(baml, frames: 2);
        var path = _temp.PathOf("mainwindow.xaml");
        app.Frames.FilePicker.Answer = path;

        app.Frames.Key(Shortcuts.SaveCode).Frame(2);
        app.Settle();

        var (fileName, filters) = app.Frames.FilePicker.LastSave;
        Assert.Equal("mainwindow.xaml", fileName);
        Assert.Equal(["*.xaml"], filters![0].Patterns);
        Assert.Contains("x:Class=\"SampleApp.MainWindow\"", File.ReadAllText(path));
    }

    [Fact]
    public void The_file_menu_shows_an_icon_beside_each_item()
    {
        var app = AppDriver.Opened();
        app.Run(2);

        app.Click("File").Frame(2);

        IconGlyph[] icons = [Icons.FileOpen, Icons.FileOpenFromFramework, Icons.FileSaveCode, Icons.FileExportProject, Icons.FileGeneratePdb,
            Icons.FileClose, Icons.FileCloseFailed, Icons.FileClearList, Icons.FileSortList, Icons.FileReloadAll];
        Assert.All(icons, icon => Assert.True(app.Frames.Drew(Text(icon))));
    }

    [Fact]
    public void An_export_asked_for_while_another_runs_waits_its_turn()
    {
        var queue = new AppStateDriver().AppState.Export;
        var first = queue.Enter(TestJobs.Named("First"));
        var second = queue.Enter(TestJobs.Named("Second"));

        Assert.True(first.IsCompleted);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, queue.WaitingCount);

        queue.Leave(first.Result);

        Assert.True(second.IsCompleted);
        Assert.Equal(0, queue.WaitingCount);
        Assert.Equal("Second", queue.Progress!.Value.Name);
    }

    [Fact]
    public void Only_the_running_export_can_end_its_turn()
    {
        var queue = new AppStateDriver().AppState.Export;
        var first = queue.Enter(TestJobs.Named("First")).Result;
        queue.Leave(first);
        var second = queue.Enter(TestJobs.Named("Second")).Result;

        queue.Leave(first);

        Assert.True(queue.IsRunning);
        Assert.False(second.Token.IsCancellationRequested);
        Assert.Equal("Second", queue.Progress!.Value.Name);
    }

    [Fact]
    public void Progress_that_arrives_after_its_turn_ended_is_dropped()
    {
        var d = new AppStateDriver();
        var queue = d.AppState.Export;
        var first = queue.Enter(TestJobs.Named("First")).Result;
        first.Progress.Report(new ExportProgress("First", 1, 1, 0.5f));
        queue.Leave(first);
        queue.Enter(TestJobs.Named("Second"));

        d.Pump();

        Assert.Equal(new ExportProgress("Second", 1, 1, null), queue.Progress);
    }

    [Fact]
    public void Progress_of_the_running_export_arrives_on_the_ui_thread()
    {
        var d = new AppStateDriver();
        var queue = d.AppState.Export;
        var start = queue.Enter(TestJobs.Named("Fixtures")).Result;

        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.75f));
        d.Pump();

        Assert.Equal(0.75f, queue.Progress!.Value.Fraction);
    }

    [Fact]
    public void Progress_that_arrives_out_of_order_never_steps_the_bar_back()
    {
        var d = new AppStateDriver();
        var queue = d.AppState.Export;
        var start = queue.Enter(TestJobs.Named("Fixtures")).Result;

        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.6f));
        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.4f));
        d.Pump();

        Assert.Equal(0.6f, queue.Progress!.Value.Fraction);
    }

    [Fact]
    public void Exports_asked_for_together_all_run_one_after_another()
    {
        var d = AppStateDriver.Opened();
        var greeter = _temp.PathOf("Greeter.cs");
        var farewell = _temp.PathOf("Farewell.cs");

        SaveCode(d.AppState, d.Fixture("Greeter"), greeter);
        SaveCode(d.AppState, d.Fixture("Farewell"), farewell);
        Assert.Equal(1, d.AppState.Export.WaitingCount);
        d.Settle();

        Assert.True(File.Exists(greeter));
        Assert.True(File.Exists(farewell));
        Assert.False(d.AppState.IsExporting);
    }

    [Fact]
    public void A_cancelled_export_ends_without_an_error()
    {
        var d = AppStateDriver.Opened();
        var path = _temp.PathOf(Path.Combine("Project", "Fixtures.csproj"));

        ExportProject(d.AppState, Assert.IsType<LoadedModule>(d.Node().Assembly), path);
        d.AppState.Export.Cancel();
        d.Settle();

        Assert.False(d.AppState.IsExporting);
        Assert.Null(d.AppState.Export.Progress);
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void The_status_bar_shows_the_export_and_how_many_wait_and_cancels_it()
    {
        var app = AppDriver.Opened();
        var start = app.AppState.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.AppState.Export.Enter(TestJobs.Named("Later"));
        start.Progress.Report(new ExportProgress("Fixtures", 2, 3, 0.5f));
        app.Run(2);

        Assert.True(app.Frames.Drew("Exporting 2/3 · Fixtures"));
        Assert.True(app.Frames.Drew("1 more queued"));
        app.Frames.Click(app.StatusText(Text(Icons.ExportCancel))).Frame();

        Assert.True(start.Token.IsCancellationRequested);
    }
    [Fact]
    public void A_finished_export_reports_what_it_wrote()
    {
        var d = AppStateDriver.Opened();
        var path = _temp.PathOf("Greeter.cs");

        SaveCode(d.AppState, d.Fixture("Greeter"), path);
        d.Settle();

        var report = Assert.Single(d.AppState.FinishedExports);
        Assert.Equal(ExportOutcome.Finished, report.Outcome);
        Assert.Equal("Greeter", report.Name);
        Assert.Equal(_temp.Path, report.Folder);
        Assert.Equal([path], report.Written);
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void A_cancelled_export_reports_that_it_was_cancelled()
    {
        var d = AppStateDriver.Opened();

        ExportProject(d.AppState, Assert.IsType<LoadedModule>(d.Node().Assembly), _temp.PathOf(Path.Combine("Project", "Fixtures.csproj")));
        d.AppState.Export.Cancel();
        d.Settle();

        var report = Assert.Single(d.AppState.FinishedExports);
        Assert.Equal(ExportOutcome.Cancelled, report.Outcome);
        Assert.Empty(report.Written);
    }

    [Fact]
    public void A_failed_export_reports_why_instead_of_logging_an_error()
    {
        var d = AppStateDriver.Opened();
        var path = _temp.PathOf(Path.Combine("missing", "Greeter.cs"));

        SaveCode(d.AppState, d.Fixture("Greeter"), path);
        d.Settle();

        var report = Assert.Single(d.AppState.FinishedExports);
        Assert.Equal(ExportOutcome.Failed, report.Outcome);
        var problem = Assert.Single(report.Problems);
        Assert.Equal("Cannot save the code of Greeter", ExportReportText.Headline(Strings.En, report.Job.Kind, problem));
        Assert.Contains("DirectoryNotFoundException", ExportReportText.DetailsOf(problem));
        Assert.Empty(d.AppState.Errors.Entries);
    }

    [Fact]
    public void The_file_menu_offers_the_export_dialog_and_no_solution_of_everything()
    {
        var app = AppDriver.Opened();
        app.Run(2);

        app.Click("File").Frame(2);

        Assert.True(app.Frames.Drew("Export…"));
        Assert.False(app.Frames.Drew("Export all as solution…"));
    }
}
