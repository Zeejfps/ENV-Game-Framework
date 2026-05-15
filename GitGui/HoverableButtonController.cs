using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

public sealed class HoverableButtonController(Action onClick, Action<bool> onHoverChanged) : KeyboardMouseController
{
    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        onHoverChanged(true);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        onHoverChanged(false);
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            onClick();
            e.Consume();
        }
    }
}