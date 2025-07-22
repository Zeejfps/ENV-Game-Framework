namespace ZGF.Gui;

public interface IKeyboardMouseController : IController
{
    Component Component { get; }

    void OnMouseEnter()
    {
    }

    void OnMouseExit()
    {
    }

    bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        return false;
    }

    void HandleMouseWheelEvent()
    {
    }

    bool OnMouseMoved(in MouseMoveEvent e)
    {
        return false;
    }

    bool HandleKeyboardKeyEvent(in KeyboardKeyEvent e)
    {
        return false;
    }

    void OnFocusLost()
    {
    }

    void OnFocusGained()
    {
    }

    bool CanReleaseFocus()
    {
        return true;
    }
}