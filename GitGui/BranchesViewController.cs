using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class BranchesViewController : KeyboardMouseController
{
    private readonly BranchesView _view;
    private readonly Context _context;

    public BranchesViewController(BranchesView view, Context context)
    {
        _view = view;
        _context = context;
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
        if (e.State != InputState.Pressed) return;
        if (e.Button == MouseButton.Left)
        {
            _view.OnClickAt(e.Mouse.Point);
            e.Consume();
            return;
        }
        if (e.Button == MouseButton.Right)
        {
            _view.OnRightClickAt(e.Mouse.Point, _context);
            e.Consume();
        }
    }
}
