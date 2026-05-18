using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class SelectableRowController : KeyboardMouseController
{
    private const int DoubleClickThresholdMs = 400;

    private readonly Action<InputModifiers> _onClick;
    private readonly Action? _onActivate;
    private readonly Action<bool> _onHoverChanged;

    private bool _hasLastClick;
    private int _lastClickTickMs;

    public SelectableRowController(
        Action<InputModifiers> onClick,
        Action<bool> onHoverChanged,
        Action? onActivate = null)
    {
        _onClick = onClick;
        _onActivate = onActivate;
        _onHoverChanged = onHoverChanged;
    }

    public override void OnMouseEnter(ref MouseEnterEvent e) => _onHoverChanged(true);
    public override void OnMouseExit(ref MouseExitEvent e) => _onHoverChanged(false);

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button == MouseButton.Left && e.State == InputState.Pressed)
        {
            _onClick(e.Modifiers);

            if (_onActivate != null)
            {
                var now = Environment.TickCount;
                var isDouble = _hasLastClick
                               && unchecked(now - _lastClickTickMs) <= DoubleClickThresholdMs;
                if (isDouble)
                {
                    _onActivate();
                    // Reset so a third click in quick succession isn't also an activation.
                    _hasLastClick = false;
                }
                else
                {
                    _lastClickTickMs = now;
                    _hasLastClick = true;
                }
            }

            e.Consume();
        }
    }
}