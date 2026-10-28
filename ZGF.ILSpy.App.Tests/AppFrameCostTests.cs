using Xunit.Abstractions;
using static ZGF.ILSpy.ScreenLayout;

namespace ZGF.ILSpy.App.Tests;

public sealed class AppFrameCostTests(ITestOutputHelper output)
{
    private const int WarmUpFrames = 120;
    private const int MeasuredFrames = 600;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_frame_of_the_app_showing_a_document_allocates_nothing(bool errorsOpen)
    {
        var app = AppDriver.Opened();
        app.Settle(2);
        app.Show(app.Fixture("Greeter"), 2);
        app.Settle(5);
        if (errorsOpen)
            OpenErrors(app);

        app.Frames.Canvas.Recording = false;
        app.Run(WarmUpFrames);
        var allocated = SteadyAllocations.PerFrame(MeasuredFrames, _ => app.Run());

        output.WriteLine($"{(errorsOpen ? "errors open" : "idle")}: {allocated:N0} B/frame allocated");
        Assert.Equal(errorsOpen, app.Ui.ErrorDialog.IsOpen);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void A_frame_of_the_app_with_only_tool_panels_open_allocates_nothing()
    {
        var app = AppDriver.Opened();
        OpenBookmarks(app.Ui);
        app.Settle(2);
        app.Ui.Layout.Close(app.Ui.Documents.PanelOf(app.AppState.Tabs.Active!));
        app.Settle(5);

        app.Frames.Canvas.Recording = false;
        app.Run(WarmUpFrames);
        var allocated = SteadyAllocations.PerFrame(MeasuredFrames, _ => app.Run());

        output.WriteLine($"tool panels only: {allocated:N0} B/frame allocated");
        Assert.Empty(app.AppState.Tabs);
        Assert.True(app.Ui.Layout.Contains(DockPanels.Bookmarks));
        Assert.Equal(0, allocated);
    }

    private static void OpenErrors(AppDriver app)
    {
        app.AppState.ReportError(new AppProblem.ResourceNotSaved("something.dll", "details"));
        app.Ui.ErrorDialog.IsOpen = true;
    }
}
