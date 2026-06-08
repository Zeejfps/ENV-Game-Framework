using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarDayCellController : KeyboardMouseController
{
    private readonly CalendarDayCell _cell;

    public CalendarDayCellController(CalendarDayCell cell)
    {
        _cell = cell;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (_cell.IsDisabled) return;
        _cell.SetHovered(true);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _cell.SetHovered(false);
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed || e.Button != MouseButton.Left) return;

        if (_cell.IsDisabled)
        {
            e.Consume();
            return;
        }

        _cell.RaiseClicked();
        e.Consume();
    }
}
