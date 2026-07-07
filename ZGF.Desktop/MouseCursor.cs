namespace ZGF.Desktop;

/// <summary>Backend-agnostic mouse cursor shapes a window can display. Maps to the platform's
/// native standard cursors so the OS renders and scales them.</summary>
public enum MouseCursor
{
    Default,
    Text,
    Hand,
    Crosshair,
    ResizeHorizontal,
    ResizeVertical,
}
