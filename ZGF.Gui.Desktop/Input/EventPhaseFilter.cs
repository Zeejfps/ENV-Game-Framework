namespace ZGF.Gui.Desktop.Input;

[Flags]
public enum EventPhaseFilter
{
    Capture = 1,
    Bubble = 2,
    Both = Capture | Bubble
}
