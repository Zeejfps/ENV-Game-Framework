using ZGF.Geometry;

namespace ZGF.Gui;

public interface IRenderer
{
    void DrawRect(RectF position, RectStyle style);
    void DrawText(RectF position, string text);
}