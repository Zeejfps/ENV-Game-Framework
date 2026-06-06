using ZGF.Gui.Mobile.Controls;
using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.Mobile.Controllers;

/// <summary>
/// Drives a <see cref="SliderView"/> from touch: jump-to-touch on press, then track the finger on
/// every move. Consumes the events so an enclosing scroll/drag gesture can't steal the slide. The
/// MobileInputSystem captures the gesture to the slider on press, so moves keep arriving here even
/// when the finger leaves the track.
/// </summary>
public sealed class SliderPointerController : MobileInputController
{
    private readonly SliderView _slider;

    public SliderPointerController(SliderView slider)
    {
        _slider = slider;
    }

    public override void OnPointerTouched(ref PointerEvent e)
    {
        _slider.SetValueFromPointerX(e.Position.X);
        e.Consume();
    }

    public override void OnPointerMoved(ref PointerEvent e)
    {
        _slider.SetValueFromPointerX(e.Position.X);
        e.Consume();
    }
}
