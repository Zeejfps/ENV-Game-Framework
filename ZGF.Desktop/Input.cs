namespace ZGF.Desktop;

/// <summary>
/// Platform-neutral key/button transition. Values match the GLFW input states so the
/// current GLFW backend can cast directly; native backends map their own states onto these.
/// </summary>
public enum InputAction
{
    Release = 0,
    Press = 1,
    Repeat = 2,
}

/// <summary>
/// Platform-neutral keyboard modifier flags. Bit values match GLFW's modifier flags (and the
/// GUI layer's InputModifiers) so the GLFW backend can cast directly.
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 0x0001,
    Control = 0x0002,
    Alt = 0x0004,
    Super = 0x0008,
    CapsLock = 0x0010,
    NumLock = 0x0020,
}

/// <summary>A window icon: tightly-packed RGBA pixels, row-major, top-left origin.</summary>
public readonly record struct WindowIconImage(int Width, int Height, byte[] Pixels);

/// <summary>A monitor's usable work area (excludes taskbars/docks), in screen coordinates.</summary>
public readonly record struct MonitorWorkArea(int X, int Y, int Width, int Height);
