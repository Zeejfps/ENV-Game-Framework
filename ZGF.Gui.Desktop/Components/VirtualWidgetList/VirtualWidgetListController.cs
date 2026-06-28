using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.VirtualWidgetList;

/// <summary>A vertically wheel-scrollable target. Lets <see cref="VirtualWidgetListController"/> drive a
/// <see cref="VirtualWidgetListView{TRow}"/> without being generic over the row type.</summary>
public interface IWheelScrollable
{
    void OnWheel(float deltaX, float deltaY);
}

/// <summary>
/// Routes mouse-wheel events to a <see cref="VirtualWidgetListView{TRow}"/> (via
/// <see cref="IWheelScrollable"/>). Attach with
/// <c>view.UseController(input, () =&gt; new VirtualWidgetListController(list))</c>.
///
/// The row widgets own their own hover/click; this controller only scrolls. It acts on the bubble
/// phase so a row that genuinely wants the wheel could consume it first, and registering it on the list
/// keeps the wheel live over both rows (it bubbles up the dispatch path) and the empty list area.
/// </summary>
public sealed class VirtualWidgetListController : KeyboardMouseController
{
    private readonly IWheelScrollable _list;

    public VirtualWidgetListController(IWheelScrollable list) => _list = list;

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        if (e.Phase != EventPhase.Bubbling) return;
        _list.OnWheel(e.DeltaX, e.DeltaY);
        e.Consume();
    }
}
