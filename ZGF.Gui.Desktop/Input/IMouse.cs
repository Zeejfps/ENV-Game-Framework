using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Input;

public interface IMouse
{
    PointF Point { get; }
    bool IsButtonPressed(MouseButton button);
}