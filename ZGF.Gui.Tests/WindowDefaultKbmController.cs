namespace ZGF.Gui.Tests;

public sealed class WindowDefaultKbmController : IKeyboardMouseController
{
    private readonly Window _window;

    public WindowDefaultKbmController(Window window)
    {
        _window = window;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }
    
    public void OnMouseEnter(ref MouseEnterEvent e) { }
    public void OnMouseExit(ref MouseExitEvent e) { }
    
    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Capturing)
            return;
        
        if (e.State == InputState.Pressed)
        {
            _window.BringToFront();
        }
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
    }

    public void OnMouseMoved(ref MouseMoveEvent e)
    {
    }

    public void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
    }

    public void OnFocusLost()
    {
    }

    public void OnFocusGained()
    {
    }

    public View View => _window;
}