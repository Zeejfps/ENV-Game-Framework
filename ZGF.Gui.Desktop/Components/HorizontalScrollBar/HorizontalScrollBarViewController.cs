using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.HorizontalScrollBar;

public sealed class HorizontalScrollBarViewController : KeyboardMouseController
{
    private readonly HorizontalScrollBarView _view;

    public HorizontalScrollBarViewController(HorizontalScrollBarView view)
    {
        _view = view;
    }

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
