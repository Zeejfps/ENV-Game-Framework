using ZGF.Desktop;
using ZGF.Geometry;

namespace ZGF.Gui.Desktop;

/// <inheritdoc cref="IWindowCoordinates"/>
public sealed class WindowCoordinates : IWindowCoordinates
{
    private readonly IWindow _window;
    private readonly IUiScale _uiScale;

    public WindowCoordinates(IWindow window, IUiScale uiScale)
    {
        _window = window;
        _uiScale = uiScale;
    }

    public ScreenPoint ToScreenPoints(CanvasPoint canvasPoint) => Space().ToScreen(canvasPoint);

    public ScreenRect ToScreenPoints(CanvasRect canvasRect) => Space().ToScreen(canvasRect);

    // Rebuilt per call: the window can have moved, been resized, or crossed onto a monitor of a
    // different scale since the last one.
    private WindowSpace Space() => WindowSpace.Of(_window, _window.ContentScale * _uiScale.Value);
}
