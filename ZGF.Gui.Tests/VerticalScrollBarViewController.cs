namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarViewController : IKeyboardMouseController
{
    private readonly VerticalScrollBarView _view;

    public VerticalScrollBarViewController(VerticalScrollBarView view)
    {
        _view = view;
    }

    public void OnEnabled(Context context)
    {
        context.InputSystem.AddInteractable(this);
    }

    public void OnDisabled(Context context)
    {
        context.InputSystem.RemoveInteractable(this);
    }

    public View View => _view;
    
    public void OnMouseEnter(ref MouseEnterEvent e)
    {
    }

    public void OnMouseExit(ref MouseExitEvent e)
    {
    }

    public void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            _view.ScrollToPoint(e.Mouse.Point);
            e.Consume();
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
}