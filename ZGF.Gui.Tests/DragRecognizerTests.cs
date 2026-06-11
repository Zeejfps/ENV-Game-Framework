using System.Numerics;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using KbmInputWidget = ZGF.Gui.Desktop.Widgets.KbmInput;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the drag recognizer contract: threshold 0 starts on press, a positive threshold arms
/// on press and starts once the cursor travels past it (the starting move emits no delta),
/// dragging steals focus and consumes moves, deltas are incremental, release ends the drag
/// and returns focus, and KbmInput's drag tier recreates the recognizer per mount.
/// </summary>
public class DragRecognizerTests
{
    private static MouseButtonEvent Button(IMouse mouse, InputState state) => new()
    {
        Mouse = mouse,
        Button = MouseButton.Left,
        State = state,
        Phase = EventPhase.Bubbling,
    };

    private static MouseMoveEvent Move(IMouse mouse) => new()
    {
        Mouse = mouse,
        Phase = EventPhase.Bubbling,
    };

    [Fact]
    public void ZeroThreshold_StartsOnPress_TracksIncrementally_EndsOnRelease()
    {
        var input = new InputSystem();
        var deltas = new List<Vector2>();
        var started = 0;
        var ended = 0;
        var recognizer = new DragRecognizer(input)
        {
            DragStarted = () => started++,
            Dragged = deltas.Add,
            DragEnded = () => ended++,
        };
        var mouse = new Mouse { Point = new PointF(10, 10) };

        var press = Button(mouse, InputState.Pressed);
        recognizer.OnMouseButtonStateChanged(ref press);
        Assert.Equal(1, started);
        Assert.True(press.IsConsumed);
        Assert.True(recognizer.IsDragging);
        Assert.Same(recognizer, input.FocusedComponent);

        mouse.Point = new PointF(15, 12);
        var move1 = Move(mouse);
        recognizer.OnMouseMoved(ref move1);
        Assert.True(move1.IsConsumed);

        mouse.Point = new PointF(18, 20);
        var move2 = Move(mouse);
        recognizer.OnMouseMoved(ref move2);

        Assert.Equal([new Vector2(5, 2), new Vector2(3, 8)], deltas);

        var release = Button(mouse, InputState.Released);
        recognizer.OnMouseButtonStateChanged(ref release);
        Assert.Equal(1, ended);
        Assert.False(recognizer.IsDragging);
        Assert.False(input.HasFocus);
    }

    [Fact]
    public void Threshold_ArmsOnPress_StartsOnlyPastTravel()
    {
        var input = new InputSystem();
        var deltas = new List<Vector2>();
        var started = 0;
        var recognizer = new DragRecognizer(input)
        {
            Threshold = 5f,
            DragStarted = () => started++,
            Dragged = deltas.Add,
        };
        var mouse = new Mouse { Point = new PointF(100, 100) };
        mouse.Press(MouseButton.Left);

        var press = Button(mouse, InputState.Pressed);
        recognizer.OnMouseButtonStateChanged(ref press);
        Assert.False(press.IsConsumed);
        Assert.False(recognizer.IsDragging);

        mouse.Point = new PointF(103, 100);
        var smallMove = Move(mouse);
        recognizer.OnMouseMoved(ref smallMove);
        Assert.Equal(0, started);
        Assert.False(smallMove.IsConsumed);

        mouse.Point = new PointF(110, 100);
        var crossingMove = Move(mouse);
        recognizer.OnMouseMoved(ref crossingMove);
        Assert.Equal(1, started);
        Assert.True(crossingMove.IsConsumed);
        Assert.Empty(deltas);

        mouse.Point = new PointF(114, 103);
        var dragMove = Move(mouse);
        recognizer.OnMouseMoved(ref dragMove);
        Assert.Equal([new Vector2(14, 3)], deltas);
    }

    [Fact]
    public void Threshold_DisarmsWhenButtonReleasedAway()
    {
        var input = new InputSystem();
        var started = 0;
        var recognizer = new DragRecognizer(input)
        {
            Threshold = 5f,
            DragStarted = () => started++,
        };
        var mouse = new Mouse { Point = new PointF(0, 0) };

        var press = Button(mouse, InputState.Pressed);
        recognizer.OnMouseButtonStateChanged(ref press);

        mouse.Point = new PointF(50, 50);
        var move = Move(mouse);
        recognizer.OnMouseMoved(ref move);

        Assert.Equal(0, started);
        Assert.False(recognizer.IsDragging);
    }

    [Fact]
    public void KbmInput_DragTier_RecreatesRecognizerPerMount()
    {
        var input = new InputSystem();
        var ctx = new Context();
        ctx.AddService(input);
        var view = new KbmInputWidget
        {
            OnDrag = _ => { },
            Child = new Box(),
        }.BuildView(ctx);
        var mouse = new Mouse { Point = new PointF(0, 0) };

        view.Mount();
        var first = Assert.IsType<DragRecognizer>(input.GetController(view));
        var press = Button(mouse, InputState.Pressed);
        first.OnMouseButtonStateChanged(ref press);
        Assert.True(first.IsDragging);

        view.Unmount();
        Assert.Null(input.GetController(view));

        view.Mount();
        var second = Assert.IsType<DragRecognizer>(input.GetController(view));
        Assert.NotSame(first, second);
        Assert.False(second.IsDragging);
    }
}
