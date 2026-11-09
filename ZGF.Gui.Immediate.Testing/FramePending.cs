namespace ZGF.Gui.Immediate.Testing;

/// <summary>What still wants a frame once one has ended.</summary>
[Flags]
public enum FramePending
{
    None = 0,

    /// <summary>The frame asked for the next at once, as an animation under way does.</summary>
    Update = 1,

    /// <summary>The frame asked for one once some time has passed, and not as ambient.</summary>
    ScheduledUpdate = 2,

    /// <summary>Work was posted, or a frame requested, from outside a frame.</summary>
    FrameRequest = 4,

    /// <summary>Tracked <see cref="BackgroundWork"/> is still running.</summary>
    BackgroundWork = 8,
}

/// <summary>How <see cref="UiFrames{TState}.Settle"/> ended: settled when nothing is
/// <see cref="Pending"/>, after <see cref="Seconds"/> of virtual time.</summary>
public readonly record struct SettleResult(FramePending Pending, double Seconds)
{
    public bool Settled => Pending == FramePending.None;
}
