namespace ZGF.Gui.Tests;

public sealed class WindowResizerDefaultKbmController : IKeyboardMouseController
{
    private readonly Window _window;
    private readonly WindowResizer _resizer;

    public WindowResizerDefaultKbmController(Window window, WindowResizer resizer)
    {
        _window = window;
        _resizer = resizer;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public void OnMouseEnter(ref MouseEnterEvent e)
    {
        _resizer.BackgroundColor = 0x9C9CCE;
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
        _resizer.BackgroundColor = 0xCECECE;
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
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

    public View View => _resizer;
}