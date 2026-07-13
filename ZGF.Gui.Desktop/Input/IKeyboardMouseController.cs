namespace ZGF.Gui.Desktop.Input;

public interface IKeyboardMouseController
{
    void OnMouseEnter(ref MouseEnterEvent e);
    void OnMouseExit(ref MouseExitEvent e);
    void OnMouseButtonStateChanged(ref MouseButtonEvent e);
    void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e);
    void OnMouseMoved(ref MouseMoveEvent e);
    void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e);
    void OnTextInput(ref TextInputEvent e);
    void OnComposition(ref CompositionEvent e);
    void OnFocusLost();
    void OnFocusGained();
}