using ZGF.ILSpy.Core;
using ZGF.ILSpy.Localization;
using static ZGF.ILSpy.Exports;
using static ZGF.ILSpy.Preferences;
using static ZGF.ILSpy.ScreenLayout;
using static ZGF.ILSpy.App.Tests.BuiltInGlyphs;

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
        var d = AppStateDriver.Opened();
        OpenBookmarks(d.Ui);

        SaveCode(d.AppState, d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.NextFrame();

        var layout = d.Ui.Layout;
        Assert.True(layout.Contains(DockPanels.Exports));
        Assert.Equal(DockPanels.Exports, layout.GroupOf(DockPanels.Exports)!.Active);
        d.Settle();
    }

    [Fact]
    public void Each_finished_export_opens_its_own_folder()
    {
        var app = AppDriver.Opened(height: TallWindow);
        var first = Folder("First");
        SaveCode(app.AppState, app.Fixture("Greeter"), Path.Combine(first, "Greeter.cs"));
        SaveCode(app.AppState, app.Fixture("Farewell"), Path.Combine(Folder("Second"), "Farewell.cs"));
        app.Settle(2);

        app.Frames.Click(app.LastTextRect("Open folder")).Frame();

        Assert.Equal([first], app.Frames.OpenedFolders);
    }

    [Fact]
    public void Show_report_opens_the_report_of_that_export_alone()
    {
        var app = AppDriver.Opened(height: TallWindow);
        var path = _temp.PathOf("Greeter.cs");
        SaveCode(app.AppState, app.Fixture("Greeter"), path);
        SaveCode(app.AppState, app.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        app.Settle(2);

        app.Frames.Click(app.LastTextRect(Icons.ExportShowReport)).Frame(2);

        var tab = app.AppState.Tabs.Active!;
        var shown = Assert.IsType<TabContent.Report>(tab.Content);
        Assert.Equal(path, Assert.Single(shown.Export.Written));
        Assert.Equal("Export report", DocumentsView.Tab(app.Ui, app.AppState, tab).Title);
        var text = ExportReportText.Write(Strings.En, [shown.Export]).GetText();
        Assert.Contains("    " + path, text);
        Assert.DoesNotContain("Farewell", text);
        Assert.Equal(2, app.AppState.FinishedExports.Count);
    }

    [Fact]
    public void A_finished_export_can_be_taken_off_the_list_and_all_cleared()
    {
        var d = AppStateDriver.Opened();
        SaveCode(d.AppState, d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        SaveCode(d.AppState, d.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        d.Settle();

        RemoveFinishedExport(d.AppState, d.AppState.FinishedExports[0]);
        Assert.Equal("Farewell", Assert.Single(d.AppState.FinishedExports).Name);
        ClearFinishedExports(d.AppState);

        Assert.Empty(d.AppState.FinishedExports);
    }

    [Fact]
    public void A_waiting_export_taken_out_of_the_queue_leaves_nothing_behind()
    {
        var d = AppStateDriver.Opened();
        var farewell = _temp.PathOf("Farewell.cs");
        SaveCode(d.AppState, d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        SaveCode(d.AppState, d.Fixture("Farewell"), farewell);
        var waiting = Assert.Single(d.AppState.Export.Waiting);

        d.AppState.Export.Withdraw(waiting.Run);
        d.Settle();

        var report = Assert.Single(d.AppState.FinishedExports);
        Assert.Equal("Greeter", report.Name);
        Assert.False(File.Exists(farewell));
    }

    [Fact]
    public void The_panel_lists_the_running_waiting_and_finished_exports()
    {
        var app = AppDriver.Opened(height: TallWindow);
        SaveCode(app.AppState, app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle();
        var running = app.AppState.Export.Enter(TestJobs.Named("Running")).Result;
        app.AppState.Export.Enter(TestJobs.Named("Later"));
        app.Run(2);

        Assert.True(app.Frames.Drew("Running"));
        Assert.True(app.Frames.Drew("Later"));
        Assert.True(app.Frames.Drew("C# project · Up next"));
        Assert.True(app.Frames.Drew("Greeter"));
        app.AppState.Export.Leave(running);
    }

    [Fact]
    public void A_cancelled_export_says_it_is_cancelling_until_it_stops()
    {
        var app = AppDriver.Opened();
        OpenExports(app.Ui);
        var start = app.AppState.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.Run(2);

        app.Click("Cancel").Frame(2);

        Assert.True(start.Token.IsCancellationRequested);
        Assert.True(app.AppState.Export.IsCancelling);
        Assert.True(app.Frames.Drew("C# project · Cancelling…"));
        app.StatusText("Cancelling…");
        app.AppState.Export.Leave(start);
        Assert.False(app.AppState.Export.IsCancelling);
    }

    [Fact]
    public void A_clean_finish_offers_the_folder_first_and_the_report_as_an_icon()
    {
        var app = AppDriver.Opened();
        SaveCode(app.AppState, app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle(2);

        Assert.True(app.Frames.Drew("Open folder"));
        Assert.False(app.Frames.Drew("Show report"));
        Assert.True(app.Frames.Drew(Text(Icons.ExportShowReport)));
    }

    [Fact]
    public void A_failed_export_offers_the_report_first()
    {
        var app = AppDriver.Opened();
        SaveCode(app.AppState, app.Fixture("Greeter"), _temp.PathOf(Path.Combine("missing", "Greeter.cs")));
        app.Settle(2);

        Assert.True(app.Frames.Drew("Show report"));
        Assert.False(app.Frames.Drew("Open folder"));
        Assert.True(app.Frames.Drew("C# file · Failed"));
    }

    [Fact]
    public void The_toolbar_counts_the_exports_and_sections_part_the_queue_and_history()
    {
        var app = AppDriver.Opened(height: TallWindow);
        SaveCode(app.AppState, app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle();
        var running = app.AppState.Export.Enter(TestJobs.Named("Running")).Result;
        app.AppState.Export.Enter(TestJobs.Named("Second"));
        app.AppState.Export.Enter(TestJobs.Named("Third"));
        app.Run(2);

        Assert.True(app.Frames.Drew("1 running · 2 queued · 1 finished"));
        Assert.True(app.Frames.Drew("Queued · 2"));
        Assert.True(app.Frames.Drew("Finished · 1"));
        Assert.True(app.Frames.Drew("C# project · 1 ahead"));
        app.AppState.Export.Leave(running);
    }

    [Fact]
    public void The_running_export_shows_its_assembly_and_percentage()
    {
        var app = AppDriver.Opened();
        OpenExports(app.Ui);
        var start = app.AppState.Export.Enter(TestJobs.Named("Both")).Result;
        start.Progress.Report(new ExportProgress("Fixtures", 2, 4, 0.5f));
        app.Run(2);

        Assert.True(app.Frames.Drew("C# project · Assembly 2 of 4 · Fixtures"));
        Assert.True(app.Frames.Drew("50%"));
        app.AppState.Export.Leave(start);
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
        var d = AppStateDriver.Opened();

        SaveCode(d.AppState, d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        var run = d.AppState.Export.Running!.Run;
        d.Settle();

        Assert.Equal(run, Assert.Single(d.AppState.FinishedExports).Run);
    }

    [Fact]
    public void The_status_bar_shows_only_the_running_export_and_opens_the_panel()
    {
        var app = AppDriver.Opened();
        var start = app.AppState.Export.Enter(TestJobs.Named("Fixtures")).Result;
        app.Run(2);
        Assert.False(app.Ui.Layout.Contains(DockPanels.Exports));

        app.Frames.Click(app.StatusText("Exporting Fixtures…")).Frame(2);

        Assert.True(app.Ui.Layout.Contains(DockPanels.Exports));
        app.AppState.Export.Leave(start);
    }

    [Fact]
    public void The_report_tab_is_written_in_the_interface_language_chosen_after_it_opened()
    {
        var app = AppDriver.Opened(height: TallWindow);
        SaveCode(app.AppState, app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle(2);
        ShowExportReport(app.AppState, app.AppState.FinishedExports[0]);
        app.Run(2);

        SetLocale(app.AppState, Locale.De);
        app.Run(2);

        var german = Strings.For(Locale.De);
        var tab = app.AppState.Tabs.Active!;
        Assert.Equal(german.ExportReportTitle, DocumentsView.Tab(app.Ui, app.AppState, tab).Title);
        Assert.Contains(german.ExportWritten, app.Ui.Documents.ViewOf(tab).Code.Document!.GetText());
    }

    [Fact]
    public void The_report_tab_stays_as_it_is_when_the_language_changes()
    {
        var d = AppStateDriver.Opened();
        SaveCode(d.AppState, d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Settle();
        ShowExportReport(d.AppState, d.AppState.FinishedExports[0]);
        var report = d.AppState.Tabs.Active!.Content;

        SetLanguage(d.AppState, DecompilationLanguage.IL);
        d.Settle();

        Assert.Same(report, d.AppState.Tabs.Active!.Content);
    }
}
