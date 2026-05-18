using ZGF.Gui;
using ZGF.Gui.Tests;

namespace GitGui;

internal sealed class CommitsViewController : KeyboardMouseController
{
    private readonly CommitsView _view;
    private CommitsView.DividerKind _activeDivider = CommitsView.DividerKind.None;
    private float _lastDragX;

    public CommitsViewController(CommitsView view)
    {
        _view = view;
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        _view.OnWheel(e.DeltaY);
        e.Consume();
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;

        if (e.State == InputState.Pressed)
        {
            var divider = _view.HitTestDivider(e.Mouse.Point);
            if (divider != CommitsView.DividerKind.None)
            {
                _activeDivider = divider;
                _lastDragX = e.Mouse.Point.X;
                _view.Context.RequestFocus(this);
                e.Consume();
                return;
            }
            _view.OnClickAt(e.Mouse.Point);
            e.Consume();
            return;
        }

        if (e.State == InputState.Released && _activeDivider != CommitsView.DividerKind.None)
        {
            _activeDivider = CommitsView.DividerKind.None;
            _view.Context.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (_activeDivider != CommitsView.DividerKind.None)
        {
            var dx = e.Mouse.Point.X - _lastDragX;
            _lastDragX = e.Mouse.Point.X;
            switch (_activeDivider)
            {
                case CommitsView.DividerKind.Author:
                    _view.ResizeAuthorColumn(dx);
                    break;
                case CommitsView.DividerKind.Hash:
                    _view.ResizeHashColumn(dx);
                    break;
                case CommitsView.DividerKind.Date:
                    _view.ResizeDateColumn(dx);
                    break;
            }
            e.Consume();
            return;
        }
        _view.SetHoveredDivider(_view.HitTestDivider(e.Mouse.Point));
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (_activeDivider == CommitsView.DividerKind.None)
        {
            _view.SetHoveredDivider(CommitsView.DividerKind.None);
        }
    }
}