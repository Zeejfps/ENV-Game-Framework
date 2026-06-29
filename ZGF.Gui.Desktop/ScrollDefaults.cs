namespace ZGF.Gui.Desktop;

/// <summary>
/// Framework defaults for pointer scrolling, shared by the scroll widgets so a consumer that does
/// not override them still gets one consistent wheel speed instead of each widget hardcoding its
/// own. Override per widget via their wheel-step properties.
/// </summary>
public static class ScrollDefaults
{
    /// <summary>Pixels travelled per mouse-wheel notch.</summary>
    public const float WheelStep = 60f;
}
