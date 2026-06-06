namespace ZGF.Gui.Desktop;

[Flags]
public enum EventPhaseFilter
{
    Capture = 1,
    Bubble = 2,
    Both = Capture | Bubble
}
