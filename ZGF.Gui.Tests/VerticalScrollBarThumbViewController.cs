namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarThumbViewController : IKeyboardMouseController
{
    public View View => _view;

    private readonly VerticalScrollBarThumbView _view;

    public VerticalScrollBarThumbViewController(VerticalScrollBarThumbView view)
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

    public void OnMouseEnter(in MouseEnterEvent e)
    {
        _view.IsSelected = true;
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        _view.IsSelected = false;
    }
}