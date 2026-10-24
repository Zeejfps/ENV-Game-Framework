using Xunit.Abstractions;
using ZGF.Gui.Immediate;
using ZGF.Gui.Immediate.Widgets;
using ZGF.Gui.Immediate.Testing;
using ZGF.ILSpy.Core.Metadata;

namespace ZGF.ILSpy.App.Tests;

public sealed class MetadataGridFrameCostTests(ITestOutputHelper output)
{
    private const int WarmUpFrames = 400;
    private const int MeasuredFrames = 400;
    private const int FramesPerSweep = 100;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_metadata_grid_with_a_row_selected_allocates_nothing_per_frame(bool scrolling)
    {
        var grid = OpenTable("TypeRef");
        var state = new MetadataGridState();
        var flags = new MetadataFilters(null);
        var frames = new UiFrames(width: 1200f, height: 300f, body: ui => Render(ui, grid, state, flags), state: Screens.Blank()) { FrameTime = 1f / 60f };
        frames.Frame();
        state.Reveal(1);
        frames.MovePointer(200f, 120f);

        frames.Canvas.Recording = false;
        for (var i = 0; i < WarmUpFrames; i++)
            Frame(frames, scrolling, i);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < MeasuredFrames; i++)
            Frame(frames, scrolling, i);
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;

        output.WriteLine($"{(scrolling ? "scrolling" : "static")}: {allocated / MeasuredFrames:N0} B/frame allocated");
        Assert.Equal(0, allocated / MeasuredFrames);
    }

    private static MetadataGrid OpenTable(string name)
    {
        var app = AppDriver.Opened(width: 1800f);
        app.Show(app.Node("Metadata", "Tables").Table(name), 3);
        return app.Workspace.Tabs.Active!.Grid!;
    }

    private static void Render(Ui<UiState> ui, MetadataGrid grid, MetadataGridState state, MetadataFilters flags)
    {
        using var screen = ui.Screen();
        ui.MetadataGridView("grid", grid, state, flags);
    }

    /// <summary>Scrolls down and back up the table in sweeps, so the measured frames show rows the
    /// warm-up already showed once and their cells' text is already worked out.</summary>
    private static void Frame(UiFrames frames, bool scrolling, int frame)
    {
        if (scrolling)
            frames.Wheel(SweepDirection(frame));
        frames.Frame();
    }

    private static int SweepDirection(int frame)
    {
        var down = frame / FramesPerSweep % 2 == 0;
        return down ? -1 : 1;
    }
}
