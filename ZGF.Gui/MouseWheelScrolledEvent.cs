namespace ZGF.Gui;

public struct MouseWheelScrolledEvent
{
    public required IMouse Mouse { get; init; }
    public required double DeltaX { get; init; }
    public required double DeltaY { get; init; }
    public bool IsConsumed { get; private set; }
    
    public void Consume()
    {
        IsConsumed = true;
    }
}