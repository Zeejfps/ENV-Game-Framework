using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Views;

namespace ZGF.Gui.Desktop.Components.Calendar;

public sealed class CalendarNavButtonController : KeyboardMouseController
{
    private readonly RectView _button;
    private readonly Action _onClick;
    private readonly uint _normalColor;
    private readonly uint _hoverColor;

    public CalendarNavButtonController(RectView button, Action onClick, uint normalColor, uint hoverColor)
    {
        _button = button;
        _onClick = onClick;
        _normalColor = normalColor;
        _hoverColor = hoverColor;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        _button.BackgroundColor = _hoverColor;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        _button.BackgroundColor = _normalColor;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.State != InputState.Pressed || e.Button != MouseButton.Left) return;

        _onClick();
        e.Consume();
    }
}
