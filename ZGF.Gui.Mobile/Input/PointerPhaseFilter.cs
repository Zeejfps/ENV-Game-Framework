namespace ZGF.Gui.Mobile.Input;

/// <summary>
/// Selects which dispatch phases a pointer controller participates in. Mirrors the desktop
/// EventPhaseFilter. Most controllers want <see cref="Both"/>; a controller that only acts as
/// an ancestor gate (e.g. a modal backdrop) can restrict itself to <see cref="Capture"/>.
/// </summary>
[Flags]
public enum PointerPhaseFilter
{
    Capture = 1,
    Bubble = 2,
    Both = Capture | Bubble,
}
