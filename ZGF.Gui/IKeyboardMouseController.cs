namespace ZGF.Gui;

public interface IKeyboardMouseController : IController
{
    View View { get; }

    void OnMouseEnter(ref MouseEnterEvent e);
    void OnMouseExit(ref MouseExitEvent e);
    void OnMouseButtonStateChanged(ref MouseButtonEvent e);
    void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e);
    void OnMouseMoved(ref MouseMoveEvent e);
    void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e);
    void OnFocusLost();
    void OnFocusGained();
    
    bool CanReleaseFocus()
    {
        return true;
    }
}