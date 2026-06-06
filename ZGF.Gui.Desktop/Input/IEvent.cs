namespace ZGF.Gui.Desktop.Input;

public interface IEvent
{
    EventPhase Phase { get; set; }
    bool IsConsumed { get; }

    void Consume();
}