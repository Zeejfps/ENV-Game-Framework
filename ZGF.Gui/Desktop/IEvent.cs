namespace ZGF.Gui.Desktop;

public interface IEvent
{
    EventPhase Phase { get; set; }
    bool IsConsumed { get; }

    void Consume();
}