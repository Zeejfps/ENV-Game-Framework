namespace ZGF.Gui.Tests;

public sealed class VerticalScrollBarViewController : KeyboardMouseController
{
    private readonly VerticalScrollBarView _view;

    public VerticalScrollBarViewController(VerticalScrollBarView view)
    {
        _view = view;
    }
    
    public override View View => _view;
    
    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling)
            return;
        
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            _view.ScrollToPoint(e.Mouse.Point);
            e.Consume();
        }
    }
}