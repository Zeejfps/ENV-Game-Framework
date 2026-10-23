using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportsPanelTests : IDisposable
{
    /// <summary>Tall enough for the panel, docked below the documents, to show every row.</summary>
    private const float TallWindow = 1400f;

    private readonly TempDir _temp = new();

    public ExportsPanelTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    private string Folder(string name)
    {
        var folder = _temp.PathOf(name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    [Fact]
    public void Starting_an_export_brings_the_exports_panel_to_the_front()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.OpenBookmarks();

        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));

        var layout = d.Workspace.Layout;
        Assert.True(layout.Contains(DockPanels.Exports));
        Assert.Equal(DockPanels.Exports, layout.GroupOf(DockPanels.Exports)!.Active);
        d.Settle();
    }

    [Fact]
    public void Each_finished_export_opens_its_own_folder()
    {
        var app = AppDriver.Opened(height: TallWindow);
        var first = Folder("First");
        app.Workspace.SaveCode(app.Fixture("Greeter"), Path.Combine(first, "Greeter.cs"));
        app.Workspace.SaveCode(app.Fixture("Farewell"), Path.Combine(Folder("Second"), "Farewell.cs"));
        app.Settle(2);

        app.Frames.Click(app.LastTextRect("Open folder")).Frame();

        Assert.Equal([first], app.Frames.OpenedFolders);
    }

    [Fact]
    public void Show_report_opens_the_report_of_that_export_alone()
    {
        var app = AppDriver.Opened(height: TallWindow);
        var path = _temp.PathOf("Greeter.cs");
        app.Workspace.SaveCode(app.Fixture("Greeter"), path);
        app.Workspace.SaveCode(app.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        app.Settle(2);

        app.Frames.Click(app.LastTextRect(LucideIcons.FileText)).Frame(2);

        var tab = app.Workspace.Tabs.Active!;
        Assert.True(tab.ShowsText);
        Assert.Equal("Export report", tab.Title);
        var text = tab.Document.GetText();
        Assert.Contains("    " + path, text);
        Assert.DoesNotContain("Farewell", text);
        Assert.Equal(2, app.Workspace.FinishedExports.Count);
    }

    [Fact]
    public void A_finished_export_can_be_taken_off_the_list_and_all_cleared()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Workspace.SaveCode(d.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        d.Settle();

        d.Workspace.RemoveFinishedExport(d.Workspace.FinishedExports[0]);
        Assert.Equal("Farewell", Assert.Single(d.Workspace.FinishedExports).Name);
        d.Workspace.ClearFinishedExports();

        Assert.Empty(d.Workspace.FinishedExports);
    }

    [Fact]
    public void A_waiting_export_taken_out_of_the_queue_leaves_nothing_behind()
    {
        var d = WorkspaceDriver.Opened();
        var farewell = _temp.PathOf("Farewell.cs");
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Workspace.SaveCode(d.Fixture("Farewell"), farewell);
        var waiting = Assert.Single(d.Workspace.Export.Waiting);

        d.Workspace.Export.Withdraw(waiting.Run);
        d.Settle();

        var report = Assert.Single(d.Workspace.FinishedExports);
        Assert.Equal("Greeter", report.Name);
        Assert.False(File.Exists(farewell));
    }

    [Fact]
    public void The_panel_lists_the_running_waiting_and_finished_exports()
    {
        var app = AppDriver.Opened(height: TallWindow);
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle();
        var running = app.Workspace.Export.Enter(TestJobs.Named("Running")).Result;
        app.Workspace.Export.Enter(TestJobs.Named("Later"));
        app.Run(2);

        Assert.True(app.Frames.Drew("Running"));
        Assert.True(app.Frames.Drew("Later"));
        Assert.True(app.Frames.Drew("C# project · Up next"));
        Assert.True(app.Frames.Drew("Greeter"));
        app.Workspace.Export.Leave(running);
    }

    [Fact]
    public void A_cancelled_export_says_it_is_cancelling_until_it_stops()
    {
        var app = AppDriver.Opened();
        app.Workspace.OpenExports();
        var start = app.Workspace.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.Run(2);

        app.Click("Cancel").Frame(2);

        Assert.True(start.Token.IsCancellationRequested);
        Assert.True(app.Workspace.Export.IsCancelling);
        Assert.True(app.Frames.Drew("C# project · Cancelling…"));
        app.StatusText("Cancelling…");
        app.Workspace.Export.Leave(start);
        Assert.False(app.Workspace.Export.IsCancelling);
    }

    [Fact]
    public void A_clean_finish_offers_the_folder_first_and_the_report_as_an_icon()
    {
        var app = AppDriver.Opened();
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle(2);

        Assert.True(app.Frames.Drew("Open folder"));
        Assert.False(app.Frames.Drew("Show report"));
        Assert.True(app.Frames.Drew(LucideIcons.FileText));
    }

    [Fact]
    public void A_failed_export_offers_the_report_first()
    {
        var app = AppDriver.Opened();
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf(Path.Combine("missing", "Greeter.cs")));
        app.Settle(2);

        Assert.True(app.Frames.Drew("Show report"));
        Assert.False(app.Frames.Drew("Open folder"));
        Assert.True(app.Frames.Drew("C# file · Failed"));
    }

    [Fact]
    public void The_toolbar_counts_the_exports_and_sections_part_the_queue_and_history()
    {
        var app = AppDriver.Opened(height: TallWindow);
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle();
        var running = app.Workspace.Export.Enter(TestJobs.Named("Running")).Result;
        app.Workspace.Export.Enter(TestJobs.Named("Second"));
        app.Workspace.Export.Enter(TestJobs.Named("Third"));
        app.Run(2);

        Assert.True(app.Frames.Drew("1 running · 2 queued · 1 finished"));
        Assert.True(app.Frames.Drew("Queued · 2"));
        Assert.True(app.Frames.Drew("Finished · 1"));
        Assert.True(app.Frames.Drew("C# project · 1 ahead"));
        app.Workspace.Export.Leave(running);
    }

    [Fact]
    public void The_running_export_shows_its_assembly_and_percentage()
    {
        var app = AppDriver.Opened();
        app.Workspace.OpenExports();
        var start = app.Workspace.Export.Enter(TestJobs.Named("Both")).Result;
        start.Progress.Report(new ExportProgress("Fixtures", 2, 4, 0.5f));
        app.Run(2);

        Assert.True(app.Frames.Drew("C# project · Assembly 2 of 4 · Fixtures"));
        Assert.True(app.Frames.Drew("50%"));
        app.Workspace.Export.Leave(start);
    }

    [Theory]
    [InlineData(8.42, "8.4 s")]
    [InlineData(68, "1m 08s")]
    [InlineData(3720, "1h 02m")]
    public void Durations_read_as_people_say_them(double seconds, string expected)
    {
        var t = ZGF.ILSpy.Localization.Strings.For(ZGF.ILSpy.Localization.Locale.En);

        Assert.Equal(expected, ExportReportText.FormatDuration(t, TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void A_finished_export_keeps_the_run_it_had_in_the_queue()
    {
        var d = WorkspaceDriver.Opened();

        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        var run = d.Workspace.Export.Running!.Run;
        d.Settle();

        Assert.Equal(run, Assert.Single(d.Workspace.FinishedExports).Run);
    }

    [Fact]
    public void The_status_bar_shows_only_the_running_export_and_opens_the_panel()
    {
        var app = AppDriver.Opened();
        var start = app.Workspace.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.Run(2);
        Assert.False(app.Workspace.Layout.Contains(DockPanels.Exports));

        app.Frames.Click(app.StatusText("Exporting Fixtures…")).Frame(2);

        Assert.True(app.Workspace.Layout.Contains(DockPanels.Exports));
        app.Workspace.Export.Leave(start);
    }

    [Fact]
    public void The_report_tab_stays_as_it_is_when_the_language_changes()
    {
        var d = WorkspaceDriver.Opened();
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Settle();
        d.Workspace.ShowExportReport(d.Workspace.FinishedExports[0]);
        var report = d.Workspace.Tabs.Active!.Document;

        d.Workspace.SetLanguage(DecompilationLanguage.IL);
        d.Settle();

        Assert.Same(report, d.Workspace.Tabs.Active!.Document);
    }
}
