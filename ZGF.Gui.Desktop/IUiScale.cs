namespace ZGF.Gui.Desktop;

/// <summary>
/// The user's chosen UI magnification, multiplied into every window's logical-point-to-device-pixel
/// factor alongside the monitor's own content scale. Observable because a change has to reach every
/// live window, not just the one the setting was changed in.
/// </summary>
public interface IUiScale
{
    float Value { get; }
    event Action<float> Changed;
}

/// <summary>An <see cref="IUiScale"/> that never changes — what a host that offers no UI-scale
/// setting gets, so the windows have one code path rather than two.</summary>
public sealed class FixedUiScale : IUiScale
{
    public static FixedUiScale One { get; } = new(1f);

    public FixedUiScale(float value)
    {
        Value = value > 0f ? value : 1f;
    }

    public float Value { get; }

    public event Action<float> Changed { add { } remove { } }
}
