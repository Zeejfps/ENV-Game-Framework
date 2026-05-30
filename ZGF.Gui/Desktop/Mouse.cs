using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

public sealed class Mouse : IMouse
{
    public PointF Point { get; set; }
    
    private readonly HashSet<MouseButton> _pressedMouseButtons = new();
    
    public void Press(MouseButton button)
    {
        _pressedMouseButtons.Add(button);
    }

    public void Release(MouseButton button)
    {
        _pressedMouseButtons.Remove(button);
    }
    
    public bool IsButtonPressed(MouseButton button)
    {
        return _pressedMouseButtons.Contains(button);
    }
}