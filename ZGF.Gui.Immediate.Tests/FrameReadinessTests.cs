namespace ZGF.Gui.Immediate.Tests;

public sealed class FrameReadinessTests
{
    [Fact]
    public void Work_that_finished_before_any_wake_reached_readiness_still_asks_for_a_frame()
    {
        var work = new BackgroundWork();
        var load = new TaskCompletionSource();
        work.Track("load", load.Task);
        load.SetResult();
        Assert.True(SpinWait.SpinUntil(() => work.Outstanding.Count == 0, TimeSpan.FromSeconds(5)));
        var readiness = new FrameReadiness(work);
        var ui = new Ui<DefaultUiState>(new DefaultUiState(), new RecordingCanvas(new SyntheticTextMeasurer()), new TestClipboard());

        Assert.Equal(FramePending.FrameRequest, readiness.Pending(ui));

        readiness.BeginFrame();
        Assert.Equal(FramePending.None, readiness.Pending(ui));
    }

    [Fact]
    public void Running_work_is_pending_and_its_finish_asks_for_a_frame()
    {
        var work = new BackgroundWork();
        var load = new TaskCompletionSource();
        work.Track("load", load.Task);
        var readiness = new FrameReadiness(work);
        var ui = new Ui<DefaultUiState>(new DefaultUiState(), new RecordingCanvas(new SyntheticTextMeasurer()), new TestClipboard());
        readiness.BeginFrame();
        Assert.Equal(FramePending.BackgroundWork, readiness.Pending(ui));

        load.SetResult();
        Assert.True(SpinWait.SpinUntil(() => work.Outstanding.Count == 0, TimeSpan.FromSeconds(5)));

        Assert.Equal(FramePending.FrameRequest, readiness.Pending(ui));
    }

    [Fact]
    public void A_scheduled_update_is_pending_until_the_clock_reaches_it()
    {
        var readiness = new FrameReadiness(null);
        var ui = new Ui<DefaultUiState>(new DefaultUiState(), new RecordingCanvas(new SyntheticTextMeasurer()), new TestClipboard());
        ui.BeginFrame(new UiInput(), 0f, 0.0, 100f, 100f);
        ui.RequestUpdateIn(1.0);
        ui.RequestAmbientUpdateIn(0.5);
        ui.EndFrame();
        readiness.EndFrame(ui);

        Assert.Equal(FramePending.ScheduledUpdate, readiness.Pending(ui));
        readiness.AdvanceTo(0.9);
        Assert.Equal(FramePending.ScheduledUpdate, readiness.Pending(ui));
        readiness.AdvanceTo(1.0);
        Assert.Equal(FramePending.None, readiness.Pending(ui));
    }
}
