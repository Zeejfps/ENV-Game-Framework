using Xunit.Abstractions;

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
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        app.Run(MeasuredFrames);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        output.WriteLine($"{(errorsOpen ? "errors open" : "idle")}: {allocated / MeasuredFrames:N0} B/frame allocated");
        Assert.Equal(errorsOpen, app.Ui.ErrorDialog.IsOpen);
        Assert.Equal(0, allocated / MeasuredFrames);
    }

    [Fact]
    public void A_frame_of_the_app_with_only_tool_panels_open_allocates_nothing()
    {
        var app = AppDriver.Opened();
        app.Ui.OpenBookmarks();
        app.Settle(2);
        app.Ui.Layout.Close(app.Workspace.Tabs.Active!.Panel);
        app.Settle(5);

        app.Frames.Canvas.Recording = false;
        app.Run(WarmUpFrames);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        app.Run(MeasuredFrames);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        output.WriteLine($"tool panels only: {allocated / MeasuredFrames:N0} B/frame allocated");
        Assert.Empty(app.Workspace.Tabs);
        Assert.True(app.Ui.Layout.Contains(DockPanels.Bookmarks));
        Assert.Equal(0, allocated / MeasuredFrames);
    }

    private static void OpenErrors(AppDriver app)
    {
        app.Workspace.ReportError("Something failed", "details");
        app.Ui.ErrorDialog.IsOpen = true;
    }
}
