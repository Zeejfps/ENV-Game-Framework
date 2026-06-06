using ZGF.Geometry;

namespace ZGF.Gui;

public interface IWindowCoordinates
{
    PointI ToScreenPoints(PointF canvasPoint);
    RectI ToScreenPoints(RectF canvasRect);
}
