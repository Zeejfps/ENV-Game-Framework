using ZGF.ILSpy.Core;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportsPanelTests : IDisposable
{
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
        var app = AppDriver.Opened();
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
        var app = AppDriver.Opened();
        var path = _temp.PathOf("Greeter.cs");
        app.Workspace.SaveCode(app.Fixture("Greeter"), path);
        app.Workspace.SaveCode(app.Fixture("Farewell"), _temp.PathOf("Farewell.cs"));
        app.Settle(2);

        app.Frames.Click(app.LastTextRect("Show report")).Frame(2);

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
    public void A_waiting_export_taken_out_of_the_queue_ends_cancelled_without_writing()
    {
        var d = WorkspaceDriver.Opened();
        var farewell = _temp.PathOf("Farewell.cs");
        d.Workspace.SaveCode(d.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        d.Workspace.SaveCode(d.Fixture("Farewell"), farewell);
        var waiting = Assert.Single(d.Workspace.Export.Waiting);

        d.Workspace.Export.Withdraw(waiting.Run);
        d.Settle();

        var reports = d.Workspace.FinishedExports;
        Assert.Equal(ExportOutcome.Cancelled, reports.Single(r => r.Name == "Farewell").Outcome);
        Assert.Equal(ExportOutcome.Finished, reports.Single(r => r.Name == "Greeter").Outcome);
        Assert.False(File.Exists(farewell));
    }

    [Fact]
    public void The_panel_lists_the_running_waiting_and_finished_exports()
    {
        var app = AppDriver.Opened();
        app.Workspace.SaveCode(app.Fixture("Greeter"), _temp.PathOf("Greeter.cs"));
        app.Settle();
        var running = app.Workspace.Export.Enter("Running").Result;
        app.Workspace.Export.Enter("Later");
        app.Run(2);

        Assert.True(app.Frames.Drew("Running"));
        Assert.True(app.Frames.Drew("Later"));
        Assert.True(app.Frames.Drew("Waiting"));
        Assert.True(app.Frames.Drew("Greeter"));
        app.Workspace.Export.Leave(running);
    }

    [Fact]
    public void The_status_bar_shows_only_the_running_export_and_opens_the_panel()
    {
        var app = AppDriver.Opened();
        var start = app.Workspace.Export.Enter("Fixtures").Result;
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
