using ZGF.Geometry;

namespace ZGF.Inputs.Mouse;

public interface IMouse
{
    PointF Point { get; }
    bool IsButtonPressed(MouseButton button);
}