using ZGF.Gui.Testing;

namespace ZGF.Gui.Immediate.Tests;

public sealed class AnimationTests
{
    [Fact]
    public void A_value_starts_at_its_target_without_animating()
    {
        var frames = new Frames();

        var value = frames.Run(0f, ui => ui.Animate("v", 5f, Tween.Linear(1f)));

        Assert.Equal(5f, value);
        Assert.False(frames.RequestedFrame);
    }

    [Fact]
    public void Tween_follows_its_curve_and_lands_exactly_on_the_target()
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f, Tween.Linear(1f)));

        Assert.Equal(0f, frames.Run(0.25f, ui => ui.Animate("v", 1f, Tween.Linear(1f))));
        Assert.True(frames.RequestedFrame);
        Assert.Equal(0.25f, frames.Run(0.25f, ui => ui.Animate("v", 1f, Tween.Linear(1f))), 4);
        Assert.Equal(0.5f, frames.Run(0.25f, ui => ui.Animate("v", 1f, Tween.Linear(1f))), 4);
        Assert.Equal(0.75f, frames.Run(0.25f, ui => ui.Animate("v", 1f, Tween.Linear(1f))), 4);
        Assert.Equal(1f, frames.Run(0.25f, ui => ui.Animate("v", 1f, Tween.Linear(1f))));
        Assert.False(frames.RequestedFrame);
    }

    [Fact]
    public void Tween_applies_its_easing_curve()
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f, Tween.OutCubic(1f)));
        frames.Run(0f, ui => ui.Animate("v", 1f, Tween.OutCubic(1f)));

        Assert.Equal(0.875f, frames.Run(0.5f, ui => ui.Animate("v", 1f, Tween.OutCubic(1f))), 4);
    }

    [Fact]
    public void Tween_holds_its_start_value_through_the_delay()
    {
        var frames = new Frames();
        var tween = Tween.Linear(1f, delay: 0.5f);
        frames.Run(0f, ui => ui.Animate("v", 0f, tween));
        frames.Run(0f, ui => ui.Animate("v", 1f, tween));

        Assert.Equal(0f, frames.Run(0.25f, ui => ui.Animate("v", 1f, tween)));
        Assert.Equal(0f, frames.Run(0.25f, ui => ui.Animate("v", 1f, tween)));
        Assert.Equal(0.25f, frames.Run(0.25f, ui => ui.Animate("v", 1f, tween)), 4);
    }

    [Fact]
    public void Retargeting_a_tween_restarts_from_the_current_value()
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f, Tween.Linear(1f)));
        frames.Run(0f, ui => ui.Animate("v", 1f, Tween.Linear(1f)));
        var midway = frames.Run(0.5f, ui => ui.Animate("v", 1f, Tween.Linear(1f)));

        var onRetarget = frames.Run(0f, ui => ui.Animate("v", 0f, Tween.Linear(1f)));
        var halfBack = frames.Run(0.5f, ui => ui.Animate("v", 0f, Tween.Linear(1f)));

        Assert.Equal(0.5f, midway, 4);
        Assert.Equal(midway, onRetarget);
        Assert.Equal(0.25f, halfBack, 4);
    }

    [Fact]
    public void Default_spring_settles_on_the_target_without_overshooting()
    {
        var (peak, final, stillAnimating) = RunSpring(Spring.Default);

        Assert.True(peak <= 1.001f, $"peak {peak}");
        Assert.Equal(1f, final);
        Assert.False(stillAnimating);
    }

    [Fact]
    public void Wobbly_spring_overshoots_then_settles()
    {
        var (peak, final, stillAnimating) = RunSpring(Spring.Wobbly);

        Assert.True(peak > 1.05f, $"peak {peak}");
        Assert.Equal(1f, final);
        Assert.False(stillAnimating);
    }

    [Fact]
    public void Retargeting_a_spring_keeps_the_value_continuous()
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f, Spring.Default));
        var last = 0f;
        for (var i = 0; i < 6; i++)
            last = frames.Run(1f / 60f, ui => ui.Animate("v", 1f, Spring.Default));

        var afterRetarget = frames.Run(1f / 60f, ui => ui.Animate("v", 0f, Spring.Default));

        Assert.InRange(last, 0.05f, 0.95f);
        Assert.True(MathF.Abs(afterRetarget - last) < 0.1f, $"jumped from {last} to {afterRetarget}");
        Assert.True(afterRetarget >= last, "a spring keeps its velocity through a retarget");
    }

    [Fact]
    public void Exponential_follow_settles_and_stops_requesting_frames()
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f));
        var value = 0f;
        for (var i = 0; i < 120 && (i == 0 || frames.RequestedFrame); i++)
            value = frames.Run(1f / 60f, ui => ui.Animate("v", 1f));

        Assert.Equal(1f, value);
        Assert.False(frames.RequestedFrame);
    }

    private static (float Peak, float Final, bool StillAnimating) RunSpring(Spring spring)
    {
        var frames = new Frames();
        frames.Run(0f, ui => ui.Animate("v", 0f, spring));
        var peak = 0f;
        var value = 0f;
        for (var i = 0; i < 600; i++)
        {
            value = frames.Run(1f / 60f, ui => ui.Animate("v", 1f, spring));
            peak = MathF.Max(peak, value);
            if (!frames.RequestedFrame) break;
        }
        return (peak, value, frames.RequestedFrame);
    }

    private sealed class Frames
    {
        private readonly UiInput _input = new();
        private readonly Ui _ui = new(new RecordingCanvas(new SyntheticTextMeasurer()), UiTheme.Dark, new NullClipboard());

        public bool RequestedFrame { get; private set; }

        public float Run(float deltaTime, Func<Ui, float> body)
        {
            _ui.BeginFrame(_input, deltaTime, 400f, 300f);
            var value = body(_ui);
            _ui.EndFrame();
            RequestedFrame = _ui.FrameRequested;
            return value;
        }
    }

    private sealed class NullClipboard : IClipboard
    {
        public void SetText(string text) { }
        public string? GetText() => null;
    }
}
