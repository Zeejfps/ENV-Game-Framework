using ZGF.Gui.Immediate.Testing;
using ZGF.Gui.Immediate.Widgets;

namespace ZGF.Gui.Immediate.Tests;

public sealed class ReadinessTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void A_spinner_does_not_hold_settling_open_but_still_wakes_the_app()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            ui.Spinner(background: 0xFF000000);
        });

        var result = frames.Settle(maxSeconds: 1);

        Assert.True(result.Settled, result.Pending.ToString());
        Assert.True(double.IsFinite(frames.WakeAt));
    }

    [Fact]
    public void A_blinking_caret_does_not_hold_settling_open()
    {
        var text = "";
        var move = true;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            ui.TextField("name", ref text);
            if (move)
                ui.MoveFocus(1);
            move = false;
        });

        var result = frames.Settle(maxSeconds: 1);

        Assert.True(frames.WantsTextInput);
        Assert.True(result.Settled, result.Pending.ToString());
    }

    [Fact]
    public void An_animation_holds_settling_open_until_it_ends()
    {
        var target = 0f;
        var value = 0f;
        var frames = new UiFrames(new DefaultUiState(), body: ui => value = ui.Animate("v", target, Tween.Linear(0.5f)));
        frames.Frame();
        target = 1f;

        var result = frames.Settle(maxSeconds: 5);

        Assert.True(result.Settled, result.Pending.ToString());
        Assert.Equal(1f, value);
        Assert.InRange(result.Seconds, 0.5, 0.6);
    }

    [Fact]
    public void An_update_scheduled_without_being_ambient_holds_settling_open_until_it_comes()
    {
        var start = double.NaN;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (double.IsNaN(start))
                start = ui.RealTime;
            if (ui.RealTime - start < 0.5)
                ui.RequestUpdateIn(0.5 - (ui.RealTime - start));
        });

        var result = frames.Settle(maxSeconds: 5);

        Assert.True(result.Settled, result.Pending.ToString());
        Assert.InRange(result.Seconds, 0.5, 0.6);
    }

    [Fact]
    public void Settling_reports_what_still_animates_once_capped()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.RequestUpdate());

        var result = frames.Settle(maxSeconds: 0.25);

        Assert.False(result.Settled);
        Assert.Equal(FramePending.Update, result.Pending);
        Assert.InRange(result.Seconds, 0.25, 0.25 + UiFrames.SettleFrameTime);
    }

    [Fact]
    public void Readiness_runs_posted_work_and_the_work_it_posts()
    {
        var log = new List<string>();
        var frames = new UiFrames(new DefaultUiState(), body: _ => { });
        var ui = frames.Run(0f, ui => ui);
        var poster = new Thread(() => ui.Post(() =>
        {
            log.Add("first");
            ui.Post(() => log.Add("second"));
        }));
        poster.Start();
        poster.Join();

        frames.WaitUntilReady(Timeout);

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public void Readiness_waits_for_a_deferred_focus_change()
    {
        var text = "";
        var move = true;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            using var screen = ui.Screen();
            ui.TextField("name", ref text);
            if (move)
                ui.MoveFocus(1);
            move = false;
        });

        frames.WaitUntilReady(Timeout);

        Assert.True(frames.WantsTextInput);
    }

    [Fact]
    public void Readiness_does_not_advance_time()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.RequestUpdateIn(0.5));
        var before = frames.RealTime;

        frames.WaitUntilReady(Timeout);

        Assert.Equal(before, frames.RealTime);
    }

    [Fact]
    public void Readiness_waits_for_tracked_work_on_another_thread_and_draws_a_frame_after_it()
    {
        var background = new BackgroundWork();
        var loaded = 0;
        var release = new ManualResetEventSlim();
        var frames = new UiFrames(new DefaultUiState(), background: background, body: ui =>
        {
            using var screen = ui.Screen();
            ui.Text(Volatile.Read(ref loaded) == 1 ? "Loaded" : "Loading");
        });
        frames.Frame();
        background.Track("font Inter", Task.Run(() =>
        {
            release.Wait();
            Volatile.Write(ref loaded, 1);
        }));
        release.Set();

        frames.WaitUntilReady(Timeout);

        Assert.True(frames.Drew("Loaded"));
        Assert.Empty(background.Outstanding);
    }

    [Fact]
    public void Awaiting_in_a_frame_continues_on_the_ui_thread_before_readiness()
    {
        var background = new BackgroundWork();
        var uiThread = Environment.CurrentManagedThreadId;
        var continuedOn = -1;
        var started = false;
        var frames = new UiFrames(new DefaultUiState(), background: background, body: ui =>
        {
            if (started) return;
            started = true;
            background.Track("load", Load());
        });

        frames.WaitUntilReady(Timeout);

        Assert.Equal(uiThread, continuedOn);
        return;

        async Task Load()
        {
            await Task.Delay(20);
            continuedOn = Environment.CurrentManagedThreadId;
        }
    }

    [Fact]
    public void Waiting_until_a_condition_holds_runs_the_frames_that_bring_it_about()
    {
        var count = 0;
        var frames = new UiFrames(new DefaultUiState(), body: _ => { });
        var ui = frames.Run(0f, ui => ui);
        Task.Run(async () =>
        {
            for (var i = 0; i < 3; i++)
            {
                await Task.Delay(10);
                ui.Post(() => count++);
            }
        });

        frames.WaitUntil(() => count == 3, Timeout);

        Assert.Equal(3, count);
    }

    [Fact]
    public void Readiness_times_out_naming_the_work_still_running()
    {
        var background = new BackgroundWork();
        var frames = new UiFrames(new DefaultUiState(), background: background, body: _ => { });
        background.Track("font Inter", new TaskCompletionSource().Task);

        var error = Assert.Throws<TimeoutException>(() => frames.WaitUntilReady(TimeSpan.FromMilliseconds(50)));

        Assert.Contains("font Inter", error.Message);
    }

    [Fact]
    public void Readiness_times_out_saying_an_animation_needs_settling()
    {
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.RequestUpdate());

        var error = Assert.Throws<TimeoutException>(() => frames.WaitUntilReady(TimeSpan.FromMilliseconds(50)));

        Assert.Contains("Settle", error.Message);
    }

    [Fact]
    public void Readiness_does_not_wait_for_a_tween_that_needs_time_and_reports_it_animating()
    {
        var target = 0f;
        var frames = new UiFrames(new DefaultUiState(), body: ui => ui.Animate("v", target, Tween.Linear(1000f)));
        frames.Frame();
        target = 1f;

        frames.WaitUntilReady(Timeout);

        Assert.Equal(0.0, frames.RealTime);
        Assert.Equal(FramePending.Animation, frames.Animating & FramePending.Animation);
        Assert.False(frames.Settle(maxSeconds: 0.1).Settled);
    }

    [Fact]
    public void Readiness_runs_a_long_finite_chain_of_updates_to_its_end()
    {
        var left = 50;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (left-- > 0)
                ui.RequestUpdate();
        });

        frames.WaitUntilReady(Timeout);

        Assert.True(left < 0);
        Assert.Equal(FramePending.None, frames.Animating);
    }

    [Fact]
    public void Readiness_reports_work_that_failed()
    {
        var background = new BackgroundWork();
        var frames = new UiFrames(new DefaultUiState(), background: background, body: _ => { });
        background.Track("font Inter", Task.FromException(new IOException("missing")));

        var error = Assert.Throws<AggregateException>(() => frames.WaitUntilReady(Timeout));

        Assert.Contains("font Inter", error.Message);
    }

    [Fact]
    public void Readiness_waits_for_work_that_finishes_only_once_the_wait_has_begun()
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var background = new BackgroundWork();
            var loaded = 0;
            var waiting = false;
            var drawnWhileWaiting = new ManualResetEventSlim();
            var frames = new UiFrames(new DefaultUiState(), background: background, body: ui =>
            {
                using var screen = ui.Screen();
                ui.Text(Volatile.Read(ref loaded) == 1 ? "Loaded" : "Loading");
                if (waiting)
                    drawnWhileWaiting.Set();
            });
            frames.Frame();
            background.Track("font Inter", Task.Run(() =>
            {
                drawnWhileWaiting.Wait();
                Volatile.Write(ref loaded, 1);
            }));
            waiting = true;

            frames.WaitUntilReady(Timeout);

            Assert.True(frames.Drew("Loaded"), $"attempt {attempt} drew no frame after the work finished");
        }
    }

    [Fact]
    public void A_frame_that_throws_before_its_body_puts_the_previous_context_back()
    {
        var original = SynchronizationContext.Current;
        var previous = new SynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(previous);
        try
        {
            var frames = new UiFrames(new DefaultUiState(), body: _ => { });
            frames.BeforeFrame = () => throw new InvalidOperationException("before");
            Assert.Throws<InvalidOperationException>(() => frames.Frame());
            Assert.Same(previous, SynchronizationContext.Current);

            frames.BeforeFrame = null;
            var ui = frames.Run(0f, ui => ui);
            ui.Post(() => throw new InvalidOperationException("posted"));
            Assert.Throws<InvalidOperationException>(() => frames.Frame());
            Assert.Same(previous, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    [Fact]
    public void Work_that_posts_itself_forever_runs_once_a_frame_and_times_readiness_out()
    {
        var runs = 0;
        var frames = new UiFrames(new DefaultUiState(), body: _ => { });
        var ui = frames.Run(0f, ui => ui);
        ui.Post(Pump);

        frames.Frame();
        Assert.Equal(1, runs);
        var error = Assert.Throws<TimeoutException>(() => frames.WaitUntilReady(TimeSpan.FromMilliseconds(50)));

        Assert.Contains("posted", error.Message);
        return;

        void Pump()
        {
            runs++;
            ui.Post(Pump);
        }
    }

    [Fact]
    public void An_update_scheduled_once_before_settling_holds_it_open_until_it_comes()
    {
        var asked = false;
        var frames = new UiFrames(new DefaultUiState(), body: ui =>
        {
            if (asked) return;
            asked = true;
            ui.RequestUpdateIn(0.5);
        });
        frames.Frame();

        var result = frames.Settle(maxSeconds: 5);

        Assert.True(result.Settled, result.Pending.ToString());
        Assert.InRange(result.Seconds, 0.5, 0.6);
    }

    [Fact]
    public void Background_work_belongs_to_one_harness()
    {
        var background = new BackgroundWork();
        _ = new UiFrames(new DefaultUiState(), background: background);

        Assert.Throws<InvalidOperationException>(() => new UiFrames(new DefaultUiState(), background: background));
    }
}
