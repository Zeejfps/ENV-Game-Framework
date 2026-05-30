using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

public interface IMouse
{
    PointF Point { get; }
    bool IsButtonPressed(MouseButton button);
}