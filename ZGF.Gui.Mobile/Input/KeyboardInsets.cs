namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// The space the on-screen keyboard currently covers, reported by the platform host (iOS keyboard
/// notifications, Android insets) and consumed by the framework. Registered in the
/// <see cref="Context"/> so keyboard-aware views can react without touching platform APIs.
/// </summary>
public sealed class KeyboardInsets
{
    /// <summary>Height in canvas points covered at the bottom of the screen; 0 when hidden.</summary>
    public float Bottom { get; private set; }

    /// <summary>Raised whenever <see cref="Bottom"/> changes.</summary>
    public event Action? Changed;

    public void SetBottom(float points)
    {
        if (Math.Abs(Bottom - points) < 0.5f)
            return;
        Bottom = points;
        Changed?.Invoke();
    }
}
