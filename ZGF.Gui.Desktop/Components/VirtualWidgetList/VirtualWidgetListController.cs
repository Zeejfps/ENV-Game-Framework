using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.VirtualWidgetList;

/// <summary>
/// Routes pointer input to a <see cref="VirtualWidgetListView{TRow}"/>: wheel scroll, hover tracking, and
/// left/right clicks (with double-click detection) raised as the list's row events. Attach with
/// <c>view.UseController(input, () =&gt; new VirtualWidgetListController&lt;TRow&gt;(list))</c>.
///
/// Clicks are handled on the <b>bubble</b> phase and consumed, so an interactive child widget inside a row
/// (e.g. a "go to" button) sees the press first and can consume it — its click then never reaches the
/// list's row-click handler. A press that no child consumed bubbles up here and becomes a row click.
/// </summary>
public sealed class VirtualWidgetListController<TRow> : KeyboardMouseController where TRow : View
{
    private readonly VirtualWidgetListView<TRow> _list;

    public VirtualWidgetListController(VirtualWidgetListView<TRow> list) => _list = list;

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        _list.OnWheel(e.DeltaX, e.DeltaY);
        e.Consume();
    }

    public override void OnMouseMoved(ref MouseMoveEvent e) => _list.OnPointerMove(e.Mouse.Point);

    public override void OnMouseExit(ref MouseExitEvent e) => _list.OnPointerExit();

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling || e.State != InputState.Pressed) return;
        if (e.Button == MouseButton.Left)
        {
            _list.OnLeftClick(e.Mouse.Point, e.Modifiers);
            e.Consume();
        }
        else if (e.Button == MouseButton.Right)
        {
            _list.OnRightClick(e.Mouse.Point);
            e.Consume();
        }
    }
}
