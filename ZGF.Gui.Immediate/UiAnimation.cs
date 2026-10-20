namespace ZGF.Gui.Immediate;

/// <summary>
/// Values that move toward a target over several frames, requesting frames until they arrive. Each
/// starts at its target, so only later changes animate. Pick the motion per call: an exponential
/// follow (the default), a <see cref="Tween"/> along an easing curve, or a <see cref="Spring"/>.
/// </summary>
public static class UiAnimation
{
    private const int FollowSalt = 0x414E494D;
    private const int TweenSalt = 0x5457454E;
    private const int SpringSalt = 0x53505247;
    private const float MaxSpringStep = 1f / 240f;

    private sealed class FollowState
    {
        public bool Started;
        public float Value;
    }

    private sealed class TweenState
    {
        public bool Started;
        public float Value;
        public float From;
        public float To;
        public double StartTime;
    }

    private sealed class SpringState
    {
        public bool Started;
        public float Value;
        public float Velocity;
    }

    public static float Animate(this Ui ui, string key, float target, float rate = 12f) =>
        ui.Animate(ui.GetId(key), target, rate);

    public static float Animate(this Ui ui, string key, float target, Tween tween) =>
        ui.Animate(ui.GetId(key), target, tween);

    public static float Animate(this Ui ui, string key, float target, Spring spring) =>
        ui.Animate(ui.GetId(key), target, spring);

    /// <summary>Exponential follow: each frame closes part of the gap, so a moving target is chased
    /// smoothly. <paramref name="rate"/> is how fast (12 settles in about a third of a second).</summary>
    public static float Animate(this Ui ui, int id, float target, float rate = 12f)
    {
        var state = ui.GetState<FollowState>(HashCode.Combine(id, FollowSalt));
        if (!state.Started)
        {
            state.Started = true;
            state.Value = target;
            return target;
        }

        if (Settled(state.Value - target, target))
        {
            state.Value = target;
            return target;
        }

        state.Value += (target - state.Value) * (1f - MathF.Exp(-rate * ui.DeltaTime));
        ui.RequestFrame();
        return state.Value;
    }

    public static float Animate(this Ui ui, int id, float target, Tween tween)
    {
        var state = ui.GetState<TweenState>(HashCode.Combine(id, TweenSalt));
        if (!state.Started)
        {
            state.Started = true;
            state.Value = state.From = state.To = target;
            return target;
        }

        if (target != state.To)
        {
            state.From = state.Value;
            state.To = target;
            state.StartTime = ui.Time;
        }

        var elapsed = (float)(ui.Time - state.StartTime) - tween.Delay;
        if (elapsed >= tween.Duration)
        {
            state.Value = state.To;
            return state.Value;
        }

        ui.RequestFrame();
        var progress = elapsed <= 0f ? 0f : elapsed / tween.Duration;
        state.Value = state.From + (state.To - state.From) * tween.Evaluate(progress);
        return state.Value;
    }

    public static float Animate(this Ui ui, int id, float target, Spring spring)
    {
        var state = ui.GetState<SpringState>(HashCode.Combine(id, SpringSalt));
        if (!state.Started)
        {
            state.Started = true;
            state.Value = target;
            return target;
        }

        if (state.Velocity == 0f && state.Value == target)
            return target;

        var dt = ui.DeltaTime;
        var steps = Math.Max(1, (int)MathF.Ceiling(dt / MaxSpringStep));
        var h = dt / steps;
        for (var i = 0; i < steps; i++)
        {
            var acceleration = (-spring.Stiffness * (state.Value - target) - spring.Damping * state.Velocity) / spring.Mass;
            state.Velocity += acceleration * h;
            state.Value += state.Velocity * h;
        }

        if (Settled(state.Value - target, target) && Settled(state.Velocity * 0.1f, target))
        {
            state.Value = target;
            state.Velocity = 0f;
            return target;
        }

        ui.RequestFrame();
        return state.Value;
    }

    private static bool Settled(float gap, float target) => MathF.Abs(gap) <= 0.001f * MathF.Max(1f, MathF.Abs(target));
}
