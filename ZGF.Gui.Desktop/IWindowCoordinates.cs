using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

public interface IWindowCoordinates
{
    PointI ToScreenPoints(PointF canvasPoint);
    RectI ToScreenPoints(RectF canvasRect);
}
