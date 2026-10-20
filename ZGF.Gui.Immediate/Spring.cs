namespace ZGF.Gui.Immediate;

/// <summary>
/// Animates a change as a damped spring pulling the value to its target. It carries its velocity
/// through a target that moves mid-flight, so it stays smooth under constant retargeting; damping
/// below <c>2 * sqrt(stiffness * mass)</c> overshoots and settles.
/// </summary>
public readonly struct Spring
{
    public Spring(float stiffness, float damping, float mass = 1f)
    {
        Stiffness = MathF.Max(0f, stiffness);
        Damping = MathF.Max(0f, damping);
        Mass = MathF.Max(0.001f, mass);
    }

    public float Stiffness { get; }
    public float Damping { get; }
    public float Mass { get; }

    /// <summary>Balanced, no visible overshoot.</summary>
    public static Spring Default => new(170f, 26f);

    /// <summary>Slow and soft, with a little overshoot.</summary>
    public static Spring Gentle => new(120f, 14f);

    /// <summary>Quick and just short of critically damped — for toggles and small controls.</summary>
    public static Spring Snappy => new(400f, 34f);

    /// <summary>Visibly bouncy.</summary>
    public static Spring Wobbly => new(180f, 12f);
}
