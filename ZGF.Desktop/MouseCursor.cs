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
    // Diagonal resize arrows, named after the compass axis they run along: Nwse points top-left to
    // bottom-right, Nesw top-right to bottom-left.
    ResizeNwse,
    ResizeNesw,
    // Arrows to all four sides: what is under the pointer is being moved.
    Move,
    // An open hand over something that can be picked up and dragged, and the hand closed on it while
    // it is held. No platform GLFW runs on has these as standard shapes, so they are drawn by ZGF.
    Grab,
    Grabbing,
}
