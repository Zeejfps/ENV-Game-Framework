using ZGF.Desktop;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

[Flags]
public enum WindowEdges
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8,
    TopLeft = Top | Left,
    TopRight = Top | Right,
    BottomLeft = Bottom | Left,
    BottomRight = Bottom | Right,
}

/// <summary>Resizes an undecorated window from a client-drawn grip along the given edges.</summary>
public sealed class WindowResizeController(IWindow window, InputSystem input, WindowEdges edges)
    : KeyboardMouseController, IProvidesCursor
{
    /// <summary>Smallest window size in screen coordinates.</summary>
    public int MinWidth { get; init; } = 1;
    public int MinHeight { get; init; } = 1;

    private bool _resizing;
    private double _grabX, _grabY;
    private int _startX, _startY, _startWidth, _startHeight;

    public MouseCursor Cursor => edges switch
    {
        WindowEdges.Left or WindowEdges.Right => MouseCursor.ResizeHorizontal,
        WindowEdges.Top or WindowEdges.Bottom => MouseCursor.ResizeVertical,
        WindowEdges.TopLeft or WindowEdges.BottomRight => MouseCursor.ResizeNwse,
        WindowEdges.TopRight or WindowEdges.BottomLeft => MouseCursor.ResizeNesw,
        _ => MouseCursor.Default,
    };

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Button != MouseButton.Left) return;
        if (e.State == InputState.Pressed)
        {
            window.GetPosition(out _startX, out _startY);
            _startWidth = window.Width;
            _startHeight = window.Height;
            ReadScreenCursor(_startX, _startY, out _grabX, out _grabY);
            _resizing = true;
            input.StealFocus(this);
            e.Consume();
        }
        else if (e.State == InputState.Released && _resizing)
        {
            _resizing = false;
            input.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (!_resizing) return;
        if (!e.Mouse.IsButtonPressed(MouseButton.Left))
        {
            _resizing = false;
            input.Blur(this);
            return;
        }
        window.GetPosition(out var x, out var y);
        ReadScreenCursor(x, y, out var cursorX, out var cursorY);
        var dx = (int)Math.Round(cursorX - _grabX);
        var dy = (int)Math.Round(cursorY - _grabY);

        int left = _startX, top = _startY, width = _startWidth, height = _startHeight;
        if (edges.HasFlag(WindowEdges.Right)) width = Math.Max(MinWidth, _startWidth + dx);
        if (edges.HasFlag(WindowEdges.Bottom)) height = Math.Max(MinHeight, _startHeight + dy);
        if (edges.HasFlag(WindowEdges.Left))
        {
            width = Math.Max(MinWidth, _startWidth - dx);
            left = _startX + _startWidth - width;
        }
        if (edges.HasFlag(WindowEdges.Top))
        {
            height = Math.Max(MinHeight, _startHeight - dy);
            top = _startY + _startHeight - height;
        }

        if (left != x || top != y) window.SetPosition(left, top);
        if (width != window.Width || height != window.Height) window.SetSize(width, height);
        e.Consume();
    }

    public override void OnFocusLost() => _resizing = false;

    private void ReadScreenCursor(int windowX, int windowY, out double x, out double y)
    {
        window.GetCursorPosition(out var cursorX, out var cursorY);
        x = windowX + cursorX;
        y = windowY + cursorY;
    }
}
