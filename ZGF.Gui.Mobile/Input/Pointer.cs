using ZGF.Geometry;

namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// The active touch point, the mobile parallel to the desktop Mouse. Single-touch for now:
/// one finger drives one logical pointer. <see cref="Point"/> is in GUI (canvas) coordinates,
/// Y-up, matching where views are laid out.
/// </summary>
public sealed class Pointer
{
    public PointF Point { get; set; }

    /// <summary>True between a pointer-down and its matching up/cancel.</summary>
    public bool IsDown { get; set; }
}
