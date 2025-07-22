namespace ZGF.Gui.Tests;

public interface IKeyboardMouseController : IController
{
    void HandleMouseEnterEvent();
    void HandleMouseExitEvent();
    bool HandleMouseButtonEvent(in MouseButtonEvent e);
    void HandleMouseWheelEvent();
    bool HandleMouseMoveEvent(in MouseMoveEvent e);
}