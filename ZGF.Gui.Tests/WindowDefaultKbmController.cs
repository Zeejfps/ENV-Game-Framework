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
    
    public void OnMouseEnter()
    {
        this.RequestFocus();
    }

    public void OnMouseExit()
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
    
    public Component Component => _window;
}