namespace ZGF.Gui;

public struct MouseWheelScrolledEvent
{
    public required IMouse Mouse { get; init; }
    public required float DeltaX { get; init; }
    public required float DeltaY { get; init; }
    public bool IsConsumed { get; private set; }
    
    public void Consume()
    {
        IsConsumed = true;
    }
}