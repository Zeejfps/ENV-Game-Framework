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

    public void OnMouseEnter(in MouseEnterEvent e)
    {
        _resizer.BackgroundColor = 0x9C9CCE;
    }

    public void OnMouseExit(in MouseExitEvent e)
    {
        _resizer.BackgroundColor = 0xCECECE;
    }
    
    public View View => _resizer;
}