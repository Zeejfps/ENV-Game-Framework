using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class DiffContentViewController : KeyboardMouseController
{
    private readonly DiffContentView _view;

    public DiffContentViewController(DiffContentView view)
    {
        _view = view;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.OnWheel(e.DeltaY);
        e.Consume();
    }
}