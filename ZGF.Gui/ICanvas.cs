using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(RectF position, RectStyle style);
    void DrawText(RectF position, string text, TextStyle style);
}