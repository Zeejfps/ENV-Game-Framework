using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class BranchesViewController : KeyboardMouseController
{
    private readonly BranchesView _view;

    public BranchesViewController(BranchesView view)
    {
        _view = view;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.OnWheel(e.DeltaY);
        e.Consume();
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        _view.SetHover(e.Mouse.Point);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _view.ClearHover();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;
        if (e.State != InputState.Pressed) return;
        _view.OnClickAt(e.Mouse.Point);
        e.Consume();
    }
}