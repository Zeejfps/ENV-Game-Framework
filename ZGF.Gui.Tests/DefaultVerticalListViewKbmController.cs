using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class DefaultVerticalListViewKbmController : IKeyboardMouseController
{
    public View View => _view;
    
    private readonly VerticalListView _view;

    public DefaultVerticalListViewKbmController(VerticalListView view)
    {
        _view = view;
    }

    public void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.Scroll(e.DeltaY * -6);
        e.Consume();
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

    public bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            if (e.Key == KeyboardKey.UpArrow)
            {
                _view.Scroll(10f);
            }
            else if (e.Key == KeyboardKey.DownArrow)
            {
                _view.Scroll(-10f);
            }
        }
        return true;
    }
}