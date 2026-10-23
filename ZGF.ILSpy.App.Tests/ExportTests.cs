using ZGF.Desktop;
using ZGF.ILSpy.Core;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly TempDir _temp = new();

    public ExportTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Saving_code_writes_the_node_in_the_current_language()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        var path = _temp.PathOf("Greeter.il");

        d.Workspace.SaveCode(d.Fixture("Greeter"), path);
        d.Settle();

        Assert.Contains(".class public auto ansi beforefieldinit ZGF.ILSpy.App.Tests.Fixtures.Greeter", File.ReadAllText(path));
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void Saving_an_assembly_as_a_project_file_exports_the_project()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf(Path.Combine("Project", "Fixtures.csproj"));

        d.Workspace.SaveCode(d.Node(), path);
        d.Settle();

        Assert.True(File.Exists(path));
        Assert.Contains(ProjectFiles(), f => Path.GetFileName(f) == "Greeter.cs");
    }

    [Fact]
    public void An_export_into_a_folder_with_other_files_waits_until_the_user_agrees()
    {
        var d = WorkspaceDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");

        d.Workspace.ExportProject(d.Node().Assembly, path);
        d.Settle();
        Assert.False(File.Exists(path));
        Assert.Equal(_temp.Path, d.Workspace.ConfirmingExport!.Folder);

        d.Workspace.FinishExportConfirmation(true);
        d.Settle();

        Assert.Null(d.Workspace.ConfirmingExport);
        Assert.True(File.Exists(path));
        Assert.Equal("keep", File.ReadAllText(_temp.PathOf("notes.txt")));
    }

    [Fact]
    public void A_declined_export_writes_nothing()
    {
        var d = WorkspaceDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");

        d.Workspace.ExportProject(d.Node().Assembly, path);
        d.Workspace.FinishExportConfirmation(false);
        d.Settle();

        Assert.Null(d.Workspace.ConfirmingExport);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Pdbs_are_generated_for_every_open_assembly_into_the_folder()
    {
        var d = WorkspaceDriver.Opened();
        var assembly = d.Node().Assembly;

        d.Workspace.GeneratePdbs(_temp.Path);
        d.Settle();

        Assert.True(File.Exists(_temp.PathOf(assembly.PdbFileName)));
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void Ctrl_S_saves_the_active_tab_where_the_user_picks()
    {
        var app = AppDriver.Opened();
        app.Show(app.Fixture("Greeter"), frames: 2);
        var path = _temp.PathOf("Greeter.cs");
        app.Frames.FilePicker.Answer = path;

        app.Frames.Key(KeyboardKey.S, KeyModifiers.Control).Frame(2);
        app.Settle();

        Assert.Equal(["Save code"], app.Frames.FilePicker.Titles);
        Assert.Contains("public class Greeter", File.ReadAllText(path));
    }

    [Fact]
    public void The_dialog_asks_before_exporting_into_a_folder_with_other_files()
    {
        var app = AppDriver.Opened();
        _temp.Write("notes.txt", "keep");
        var path = _temp.PathOf("Fixtures.csproj");
        app.Workspace.ExportProject(app.Node().Assembly, path);
        app.Run(2);

        Assert.True(app.Frames.Drew("Folder is not empty"));
        app.Click("Export").Frame(2);
        app.Settle(1);

        Assert.True(File.Exists(path));
        Assert.False(app.Frames.Drew("Folder is not empty"));
    }

    [Fact]
    public void The_file_menu_shows_an_icon_beside_each_item()
    {
        var app = AppDriver.Opened();
        app.Run(2);

        app.Click("File").Frame(2);

        string[] icons = [LucideIcons.FolderOpen, LucideIcons.Library, LucideIcons.Save, LucideIcons.FolderOutput, LucideIcons.Bug,
            LucideIcons.X, LucideIcons.ListX, LucideIcons.RotateCcw, LucideIcons.LogOut];
        Assert.All(icons, icon => Assert.True(app.Frames.Drew(icon)));
    }

    [Fact]
    public void An_export_asked_for_while_another_runs_waits_its_turn()
    {
        var queue = new WorkspaceDriver().Workspace.Export;
        var first = queue.Enter("First");
        var second = queue.Enter("Second");

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
        var queue = new WorkspaceDriver().Workspace.Export;
        var first = queue.Enter("First").Result;
        queue.Leave(first);
        var second = queue.Enter("Second").Result;

        queue.Leave(first);

        Assert.True(queue.IsRunning);
        Assert.False(second.Token.IsCancellationRequested);
        Assert.Equal("Second", queue.Progress!.Value.Name);
    }

    [Fact]
    public void Progress_that_arrives_after_its_turn_ended_is_dropped()
    {
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
        var first = queue.Enter("First").Result;
        first.Progress.Report(new ExportProgress("First", 1, 1, 0.5f));
        queue.Leave(first);
        queue.Enter("Second");

        d.Pump();

        Assert.Equal(new ExportProgress("Second", 1, 1, null), queue.Progress);
    }

    [Fact]
    public void Progress_of_the_running_export_arrives_on_the_ui_thread()
    {
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
        var start = queue.Enter("Fixtures").Result;

        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.75f));
        d.Pump();

        Assert.Equal(0.75f, queue.Progress!.Value.Fraction);
    }

    [Fact]
    public void Progress_that_arrives_out_of_order_never_steps_the_bar_back()
    {
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
        var start = queue.Enter("Fixtures").Result;

        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.6f));
        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.4f));
        d.Pump();

        Assert.Equal(0.6f, queue.Progress!.Value.Fraction);
    }

    [Fact]
    public void Exports_asked_for_together_all_run_one_after_another()
    {
        var d = WorkspaceDriver.Opened();
        var greeter = _temp.PathOf("Greeter.cs");
        var farewell = _temp.PathOf("Farewell.cs");

        d.Workspace.SaveCode(d.Fixture("Greeter"), greeter);
        d.Workspace.SaveCode(d.Fixture("Farewell"), farewell);
        Assert.Equal(1, d.Workspace.Export.WaitingCount);
        d.Settle();

        Assert.True(File.Exists(greeter));
        Assert.True(File.Exists(farewell));
        Assert.False(d.Workspace.IsExporting);
    }

    [Fact]
    public void A_cancelled_export_ends_without_an_error()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf(Path.Combine("Project", "Fixtures.csproj"));

        d.Workspace.ExportProject(d.Node().Assembly, path);
        d.Workspace.Export.Cancel();
        d.Settle();

        Assert.False(d.Workspace.IsExporting);
        Assert.Null(d.Workspace.Export.Progress);
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void The_status_bar_shows_the_export_and_how_many_wait_and_cancels_it()
    {
        var app = AppDriver.Opened();
        var start = app.Workspace.Export.Enter("Fixtures").Result;
        app.Workspace.Export.Enter("Later");
        start.Progress.Report(new ExportProgress("Fixtures", 2, 3, 0.5f));
        app.Run(2);

        Assert.True(app.Frames.Drew("Exporting 2/3 · Fixtures"));
        Assert.True(app.Frames.Drew("1 more queued"));
        app.Frames.Click(app.StatusText(LucideIcons.X)).Frame();

        Assert.True(start.Token.IsCancellationRequested);
    }
    [Fact]
    public void A_finished_export_reports_what_it_wrote()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf("Greeter.cs");

        d.Workspace.SaveCode(d.Fixture("Greeter"), path);
        d.Settle();

        var report = Assert.Single(d.Workspace.FinishedExports);
        Assert.Equal(ExportOutcome.Finished, report.Outcome);
        Assert.Equal("Greeter", report.Name);
        Assert.Equal(_temp.Path, report.Folder);
        Assert.Equal([path], report.Written);
        Assert.Empty(report.Problems);
    }

    [Fact]
    public void A_cancelled_export_reports_that_it_was_cancelled()
    {
        var d = WorkspaceDriver.Opened();

        d.Workspace.ExportProject(d.Node().Assembly, _temp.PathOf(Path.Combine("Project", "Fixtures.csproj")));
        d.Workspace.Export.Cancel();
        d.Settle();

        var report = Assert.Single(d.Workspace.FinishedExports);
        Assert.Equal(ExportOutcome.Cancelled, report.Outcome);
        Assert.Empty(report.Written);
    }

    [Fact]
    public void A_failed_export_reports_why_instead_of_logging_an_error()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf(Path.Combine("missing", "Greeter.cs"));

        d.Workspace.SaveCode(d.Fixture("Greeter"), path);
        d.Settle();

        var report = Assert.Single(d.Workspace.FinishedExports);
        Assert.Equal(ExportOutcome.Failed, report.Outcome);
        var problem = Assert.Single(report.Problems);
        Assert.Equal("Cannot save the code of Greeter", problem.Headline);
        Assert.Contains("DirectoryNotFoundException", problem.Details);
        Assert.Empty(d.Workspace.Errors.Entries);
    }

    [Fact]
    public void The_notice_opens_the_folder_the_export_wrote_into()
    {
        var app = AppDriver.Opened();
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle(2);

        Assert.True(app.Frames.Drew("Exported Greeter"));
        app.Click("Open folder").Frame();

        Assert.Equal([_temp.Path], app.Frames.OpenedFolders);
    }

    [Fact]
    public void The_notice_shows_the_report_in_a_tab_of_its_own()
    {
        var app = AppDriver.Opened();
        var path = _temp.PathOf("Greeter.cs");
        app.Workspace.SaveCode(app.Fixture("Greeter"), path);
        app.Settle(2);
        var tabs = app.Workspace.Tabs.Count();

        app.Click("Show report").Frame(2);

        var tab = app.Workspace.Tabs.Active!;
        Assert.Equal(tabs + 1, app.Workspace.Tabs.Count());
        Assert.True(tab.ShowsText);
        Assert.Equal("Export report", tab.Title);
        Assert.Contains("    " + path, tab.Document.GetText());
        Assert.Empty(app.Workspace.FinishedExports);
        Assert.False(app.Frames.Drew("Exported Greeter"));
    }

    [Fact]
    public void Several_finished_exports_share_one_notice_and_report()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Workspace.SaveCode(d.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        d.Settle();

        d.Workspace.ShowExportReport();

        var text = d.Workspace.Tabs.Active!.Document.GetText();
        Assert.Contains("Exported Greeter", text);
        Assert.Contains("Exported Farewell", text);
    }

    [Fact]
    public void The_report_tab_stays_as_it_is_when_the_language_changes()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Settle();
        d.Workspace.ShowExportReport();
        var report = d.Workspace.Tabs.Active!.Document;

        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Settle();

        Assert.Same(report, d.Workspace.Tabs.Active!.Document);
    }

    [Fact]
    public void Dismissing_the_notice_drops_the_reports()
    {
        var app = AppDriver.Opened();
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle(2);

        app.Frames.Click(app.StatusText(LucideIcons.X)).Frame(2);

        Assert.Empty(app.Workspace.FinishedExports);
        Assert.False(app.Frames.Drew("Exported Greeter"));
    }

    private IEnumerable<string> ProjectFiles() => Directory.EnumerateFiles(_temp.Path, "*.cs", SearchOption.AllDirectories);
}
