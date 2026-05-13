namespace ZGF.Gui;

public interface IEvent
{
    EventPhase Phase { get; set; }
    bool IsConsumed { get; }

    void Consume();
}