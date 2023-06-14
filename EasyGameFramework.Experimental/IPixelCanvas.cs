using System.Numerics;

namespace EasyGameFramework.Experimental;

public interface IPixelCanvas
{
    void Clear();
    void DrawLine(int x0, int y0, int x1, int y1);
    void DrawRect(int x, int y, int width, int height);
    void Render();
    Vector2 ScreenToCanvasPoint(Vector2 screenPoint);
}