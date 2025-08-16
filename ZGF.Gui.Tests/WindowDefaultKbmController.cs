namespace ZGF.Gui.Tests;

public sealed class WindowDefaultKbmController : KeyboardMouseController
{
    private readonly Window _window;

    public WindowDefaultKbmController(Window window)
    {
        _window = window;
    }
    
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Capturing)
            return;
        
        if (e.State == InputState.Pressed)
        {
            _window.BringToFront();
        }
    }
    
    public override View View => _window;
}