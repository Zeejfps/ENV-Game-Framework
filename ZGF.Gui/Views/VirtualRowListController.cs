using ZGF.Gui.Tests;

namespace ZGF.Gui;

/// <summary>
/// Translates raw mouse events into <see cref="VirtualRowListView"/>'s hit-test +
/// scroll API. Kept separate from the widget so the input source is swappable and the
/// widget itself stays framework-agnostic for tests.
/// </summary>
public sealed class VirtualRowListController : KeyboardMouseController
{
    private readonly VirtualRowListView _list;

    public VirtualRowListController(VirtualRowListView list)
    {
        _list = list;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _list.OnWheel(e.DeltaX, e.DeltaY);
        e.Consume();
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        _list.OnPointerMove(e.Mouse.Point);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _list.OnPointerExit();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (e.Button == MouseButton.Left)
        {
            _list.OnLeftClick(e.Mouse.Point, e.Modifiers);
            e.Consume();
            return;
        }
        if (e.Button == MouseButton.Right)
        {
            _list.OnRightClick(e.Mouse.Point);
            e.Consume();
        }
    }
}
