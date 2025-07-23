namespace ZGF.Gui;

public interface IKeyboardMouseController : IController
{
    View View { get; }

    void OnMouseEnter(in MouseEnterEvent e);

    void OnMouseExit(in MouseExitEvent e);

    bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        return false;
    }

    void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    bool OnMouseMoved(in MouseMoveEvent e)
    {
        return false;
    }

    bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
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