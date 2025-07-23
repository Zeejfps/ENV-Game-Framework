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
    
    public void OnMouseEnter(in MouseEnterEvent e)
    {
        this.RequestFocus();
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        this.Blur();
    }
    
    public bool OnMouseButtonStateChanged(in MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            _window.BringToFront();
        }
        return true;
    }
    
    public View View => _window;
}