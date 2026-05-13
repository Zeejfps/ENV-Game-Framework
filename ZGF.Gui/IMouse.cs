using ZGF.Geometry;

namespace ZGF.Gui;

public interface IMouse
{
    PointF Point { get; }
    bool IsButtonPressed(MouseButton button);
}