using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Desktop.Widgets;
using ZGF.Gui.Views;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the KbmInput widget contract: handlers register a controller on the child's view for
/// its mounted lifetime, semantic callbacks fire once per gesture (bubble phase) and click
/// consumes, raw handlers see unconsumed events, and the Controller seam receives the built
/// child view.
/// </summary>
public class KbmInputTests
{
    private static Context ContextWith(InputSystem input)
    {
        var ctx = new Context();
        ctx.AddService(input);
        return ctx;
    }

    private static MouseButtonEvent LeftPress(EventPhase phase) => new()
    {
        Mouse = new Mouse(),
        Button = MouseButton.Left,
        State = InputState.Pressed,
        Phase = phase,
    };

    [Fact]
    public void Handlers_RegisterOnMount_UnregisterOnUnmount()
    {
        var input = new InputSystem();
        var view = new KbmInput
        {
            OnClick = () => { },
            Child = new Box(),
        }.BuildView(ContextWith(input));

        Assert.Null(input.GetController(view));

        view.Mount();
        Assert.NotNull(input.GetController(view));

        view.Unmount();
        Assert.Null(input.GetController(view));
    }

    [Fact]
    public void NoHandlersNoController_RegistersNothing()
    {
        var input = new InputSystem();
        var view = new KbmInput { Child = new Box() }.BuildView(ContextWith(input));

        view.Mount();

        Assert.Null(input.GetController(view));
    }

    [Fact]
    public void Click_FiresOnBubblePhase_AndConsumes()
    {
        var input = new InputSystem();
        var clicks = 0;
        var view = new KbmInput
        {
            OnClick = () => clicks++,
            Child = new Box(),
        }.BuildView(ContextWith(input));
        view.Mount();
        var controller = input.GetController(view)!;

        var capture = LeftPress(EventPhase.Capturing);
        controller.OnMouseButtonStateChanged(ref capture);
        Assert.Equal(0, clicks);
        Assert.False(capture.IsConsumed);

        var bubble = LeftPress(EventPhase.Bubbling);
        controller.OnMouseButtonStateChanged(ref bubble);
        Assert.Equal(1, clicks);
        Assert.True(bubble.IsConsumed);
    }

    [Fact]
    public void Hover_FiresOncePerGesture()
    {
        var input = new InputSystem();
        var enters = 0;
        var exits = 0;
        var view = new KbmInput
        {
            OnHoverEnter = () => enters++,
            OnHoverExit = () => exits++,
            Child = new Box(),
        }.BuildView(ContextWith(input));
        view.Mount();
        var controller = input.GetController(view)!;

        var enterCapture = new MouseEnterEvent { Mouse = new Mouse(), Phase = EventPhase.Capturing };
        controller.OnMouseEnter(ref enterCapture);
        var enterBubble = new MouseEnterEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller.OnMouseEnter(ref enterBubble);

        var exitCapture = new MouseExitEvent { Mouse = new Mouse(), Phase = EventPhase.Capturing };
        controller.OnMouseExit(ref exitCapture);
        var exitBubble = new MouseExitEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller.OnMouseExit(ref exitBubble);

        Assert.Equal(1, enters);
        Assert.Equal(1, exits);
    }

    [Fact]
    public void RawHandler_SeesEvent_UnlessClickConsumedIt()
    {
        var input = new InputSystem();
        var rawCalls = 0;
        var view = new KbmInput
        {
            OnClick = () => { },
            OnMouseButton = (ref MouseButtonEvent _) => rawCalls++,
            Child = new Box(),
        }.BuildView(ContextWith(input));
        view.Mount();
        var controller = input.GetController(view)!;

        var capture = LeftPress(EventPhase.Capturing);
        controller.OnMouseButtonStateChanged(ref capture);
        Assert.Equal(1, rawCalls);

        var bubble = LeftPress(EventPhase.Bubbling);
        controller.OnMouseButtonStateChanged(ref bubble);
        Assert.Equal(1, rawCalls);
    }

    private sealed class StatefulController : KeyboardMouseController, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void ControllerSeam_ReceivesBuiltChildView_AndFollowsMountedLifetime()
    {
        var input = new InputSystem();
        View? received = null;
        var instances = new List<StatefulController>();
        var view = new KbmInput
        {
            Controller = v =>
            {
                received = v;
                var c = new StatefulController();
                instances.Add(c);
                return c;
            },
            Child = new Box(),
        }.BuildView(ContextWith(input));

        view.Mount();
        Assert.Same(view, received);
        Assert.Same(instances[0], input.GetController(view));

        view.Unmount();
        view.Mount();
        Assert.Equal(2, instances.Count);
        Assert.True(instances[0].IsDisposed);
        Assert.False(instances[1].IsDisposed);
    }
}
