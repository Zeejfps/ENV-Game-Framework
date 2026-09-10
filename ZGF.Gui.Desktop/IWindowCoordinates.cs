using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

/// <summary>Places a view's own coordinates on the desktop, for anything that has to sit beside it in
/// a window of its own — a menu, a tooltip, the OS's IME candidate list.</summary>
public interface IWindowCoordinates
{
    ScreenPoint ToScreenPoints(CanvasPoint canvasPoint);
    ScreenRect ToScreenPoints(CanvasRect canvasRect);
}
