using ZGF.Desktop;
using ZGF.Gui.Immediate;
using ZGF.KeyboardModule;

namespace ZGF.ILSpy.App.Tests;

public sealed class ExportsPanelInteractionTests : IDisposable
{
    private const float TallWindow = 1400f;

    private readonly TempDir _temp = new();

    public ExportsPanelInteractionTests()
    {
        Directory.CreateDirectory(_temp.Path);
    }

    public void Dispose() => _temp.Dispose();

    /// <summary>An app with two finished saves, Greeter then Farewell, in folders of their own.</summary>
    private AppDriver TwoFinished(float width = 1000f)
    {
        var app = AppDriver.Opened(width: width, height: TallWindow);
        app.Workspace.SaveCode(app.Fixture("Greeter"), Path.Combine(Folder("First"), "Greeter.cs"));
        app.Workspace.SaveCode(app.Fixture("Farewell"), Path.Combine(Folder("Second"), "Farewell.cs"));
        app.Settle(2);
        return app;
    }

    private string Folder(string name)
    {
        var folder = _temp.PathOf(name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static int RunOf(AppDriver app, string name) => app.Workspace.FinishedExports.Single(r => r.Name == name).Run;

    [Fact]
    public void Clicking_a_row_selects_it_and_the_arrow_keys_walk_the_list()
    {
        var app = TwoFinished();

        app.Frames.Wait(1).Click(app.TextRect("Farewell")).Frame();
        Assert.Equal(RunOf(app, "Farewell"), app.Workspace.SelectedExport);

        app.Frames.Key(KeyboardKey.DownArrow).Frame();
        Assert.Equal(RunOf(app, "Greeter"), app.Workspace.SelectedExport);
        app.Frames.Key(KeyboardKey.UpArrow).Frame();
        Assert.Equal(RunOf(app, "Farewell"), app.Workspace.SelectedExport);
    }

    [Fact]
    public void Enter_and_a_double_click_open_the_report_of_a_finished_export()
    {
        var app = TwoFinished();
        app.Frames.Wait(1).Click(app.TextRect("Greeter")).Frame();

        app.Frames.Key(KeyboardKey.Enter).Frame(2);
        Assert.Equal("Export report", app.Workspace.Tabs.Active!.Title);
        var tabs = app.Workspace.Tabs.Count();

        app.Frames.Wait(1).DoubleClick(app.TextRect("Farewell")).Frame(2);
        Assert.Equal(tabs + 1, app.Workspace.Tabs.Count());
    }

    [Fact]
    public void Delete_takes_the_selected_export_off_the_history()
    {
        var app = TwoFinished();
        app.Frames.Wait(1).Click(app.TextRect("Greeter")).Frame();

        app.Frames.Key(KeyboardKey.Delete).Frame();

        Assert.Equal("Farewell", Assert.Single(app.Workspace.FinishedExports).Name);
    }

    [Fact]
    public void Ctrl_C_copies_the_folder_of_the_selected_export()
    {
        var app = TwoFinished();
        app.Frames.Wait(1).Click(app.TextRect("Greeter")).Frame();

        app.Frames.Key(KeyboardKey.C, KeyModifiers.Control).Frame();

        Assert.Equal(_temp.PathOf("First"), app.Frames.Clipboard.Text);
    }

    [Fact]
    public void The_context_menu_offers_what_suits_a_finished_export()
    {
        var app = TwoFinished();

        app.Frames.Wait(1).Click(app.TextRect("Greeter"), button: PointerButton.Right).Frame(2);
        Assert.True(app.Frames.Drew("Copy folder path"));
        app.ClickMenuItem("Remove from history").Frame();

        Assert.Equal("Farewell", Assert.Single(app.Workspace.FinishedExports).Name);
    }

    [Fact]
    public void Shift_F10_opens_the_menu_of_the_selected_export()
    {
        var app = TwoFinished();
        app.Frames.Wait(1).Click(app.TextRect("Greeter")).Frame();

        app.Frames.Key(KeyboardKey.F10, KeyModifiers.Shift).Frame(2);

        Assert.True(app.Frames.Drew("Copy folder path"));
        Assert.Equal(RunOf(app, "Greeter"), app.Workspace.ExportMenuTarget);
    }

    [Fact]
    public void A_waiting_export_is_withdrawn_from_the_keyboard_and_the_running_one_stays()
    {
        var app = AppDriver.Opened(height: TallWindow);
        app.Workspace.OpenExports();
        var running = app.Workspace.Export.Enter(TestJobs.Named("Running")).Result;
        app.Workspace.Export.Enter(TestJobs.Named("Later"));
        app.Run(2);

        app.Frames.Wait(1).Click(app.TextRect("Running")).Frame();
        app.Frames.Key(KeyboardKey.Delete).Frame();
        Assert.True(app.Workspace.Export.IsRunning);
        Assert.Equal(1, app.Workspace.Export.WaitingCount);

        app.Frames.Key(KeyboardKey.DownArrow).Frame();
        app.Frames.Key(KeyboardKey.Delete).Frame();
        Assert.Equal(0, app.Workspace.Export.WaitingCount);
        app.Workspace.Export.Leave(running);
    }

    [Fact]
    public void A_narrow_panel_shows_its_actions_as_icons()
    {
        var app = TwoFinished(width: 560f);
        app.Run(2);

        Assert.False(app.Frames.Drew("Open folder"));
        Assert.True(app.Frames.Drew(LucideIcons.FolderOpen));
    }

    [Fact]
    public void The_running_export_keeps_the_frames_coming_so_its_spinner_turns()
    {
        var app = AppDriver.Opened();
        app.Workspace.OpenExports();
        var running = app.Workspace.Export.Enter(TestJobs.Named("Running")).Result;
        app.Run(2);

        Assert.False(app.Frames.Drew(LucideIcons.Loader2));
        Assert.True(app.Frames.WakeAt - app.Frames.RealTime <= 1.0 / 30.0 + 1e-6);
        app.Workspace.Export.Leave(running);
    }
}
