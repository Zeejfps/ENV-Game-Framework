namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// Dispatch phase for a pointer event as it travels the view tree. Mirrors the desktop
/// EventPhase: capturing runs ancestor-first (root → target), bubbling runs target → root.
/// </summary>
public enum PointerPhase
{
    Capturing,
    Bubbling,
}
