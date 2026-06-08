using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarNavButtonController : KeyboardMouseController
{
    private readonly Action _onClick;

    public CalendarNavButtonController(Action onClick)
    {
        _onClick = onClick;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed || e.Button != MouseButton.Left) return;

        _onClick();
        e.Consume();
    }
}
