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

    public void OnMouseEnter()
    {
        _resizer.BackgroundColor = 0x9C9CCE;
    }

    public void OnMouseExit()
    {
        _resizer.BackgroundColor = 0xCECECE;
    }
    
    public Component Component => _resizer;
}