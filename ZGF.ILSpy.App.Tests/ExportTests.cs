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
    public void Saving_an_assembly_writes_one_code_file_even_under_a_project_name()
    {
        var d = WorkspaceDriver.Opened();
        var path = _temp.PathOf("Fixtures.csproj");

        d.Workspace.SaveCode(d.Node(), path);
        d.Settle();

        var code = File.ReadAllText(path);
        Assert.Contains("public class Greeter", code);
        Assert.False(code.TrimStart().StartsWith("<Project", StringComparison.Ordinal));
        Assert.Equal([path], Directory.EnumerateFiles(_temp.Path));
    }

    [Fact]
    public void Saving_a_resource_writes_its_bytes_as_they_are()
    {
        var d = WorkspaceDriver.Opened();
        var node = d.Node("Resources", "data.json");
        var path = _temp.PathOf("data.json");

        Assert.True(d.Workspace.CanSaveCode(node));
        d.Workspace.SaveCode(node, path);

        Assert.Equal(node.ReadResource(), File.ReadAllBytes(path));
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
    public void Ctrl_S_on_baml_offers_to_save_it_as_xaml()
    {
        var app = AppDriver.Opened();
        var sample = Path.Combine(AppContext.BaseDirectory, "Fixtures", "SampleApp", "SampleApp.dll");
        app.Workspace.Open(sample);
        app.Driver.RunUntil(() => app.Workspace.Assemblies.Assemblies.Any(a => a.FilePath == sample));
        var assembly = app.Workspace.Assemblies.Assemblies.Single(a => a.FilePath == sample);
        var baml = assembly.Root.Child("Resources", "SampleApp.g.resources", "mainwindow.baml");
        app.Show(baml, frames: 2);
        var path = _temp.PathOf("mainwindow.xaml");
        app.Frames.FilePicker.Answer = path;

        app.Frames.Key(KeyboardKey.S, KeyModifiers.Control).Frame(2);
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

        string[] icons = [LucideIcons.FolderOpen, LucideIcons.Library, LucideIcons.Save, LucideIcons.FolderOutput, LucideIcons.Bug,
            LucideIcons.X, LucideIcons.ListX, LucideIcons.RotateCcw, LucideIcons.LogOut];
        Assert.All(icons, icon => Assert.True(app.Frames.Drew(icon)));
    }

    [Fact]
    public void An_export_asked_for_while_another_runs_waits_its_turn()
    {
        var queue = new WorkspaceDriver().Workspace.Export;
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
        var queue = new WorkspaceDriver().Workspace.Export;
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
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
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
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
        var start = queue.Enter(TestJobs.Named("Fixtures")).Result;

        start.Progress.Report(new ExportProgress("Fixtures", 1, 1, 0.75f));
        d.Pump();

        Assert.Equal(0.75f, queue.Progress!.Value.Fraction);
    }

    [Fact]
    public void Progress_that_arrives_out_of_order_never_steps_the_bar_back()
    {
        var d = new WorkspaceDriver();
        var queue = d.Workspace.Export;
        var start = queue.Enter(TestJobs.Named("Fixtures")).Result;

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
        var start = app.Workspace.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.Workspace.Export.Enter(TestJobs.Named("Later"));
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
    public void The_file_menu_offers_the_export_dialog_and_no_solution_of_everything()
    {
        var app = AppDriver.Opened();
        app.Run(2);

        app.Click("File").Frame(2);

        Assert.True(app.Frames.Drew("Export…"));
        Assert.False(app.Frames.Drew("Export all as solution…"));
    }
}
