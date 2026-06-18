using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using ZGF.Observable;

namespace ZGF.Gui.Tests;

/// <summary>
/// Pins the pressable seam: <see cref="KbmController"/> drives an <see cref="IInteractableWidget"/>'s
/// hover/press/activation on the bubble phase only, and <c>WithController&lt;KbmController&gt;(target)</c>
/// DI-builds the controller with that target injected, registered for the view's mounted lifetime.
/// </summary>
public class KbmControllerTests
{
    private sealed class FakeInteractable : IInteractableWidget
    {
        public State<bool> Hovered { get; } = new(false);
        public State<bool> Pressed { get; } = new(false);
        public State<bool> Enabled { get; } = new(true);
        public int Activations { get; private set; }

        IWritable<bool> IInteractableWidget.Hovered => Hovered;
        IWritable<bool> IInteractableWidget.Pressed => Pressed;
        IReadable<bool> IInteractableWidget.Enabled => Enabled;

        public FakeInteractable()
        {
            Pressed.Changed += pressed =>
            {
                if (pressed) Activations++;
            };
        }

        public View BuildView(Context ctx) => new Box().BuildView(ctx);
    }

    private static Context ContextWith(InputSystem input)
    {
        var ctx = new Context();
        ctx.AddService(input);
        return ctx;
    }

    [Fact]
    public void Press_ActivatesOnBubbleOnly_AndTracksHoverAndPressed()
    {
        var target = new FakeInteractable();
        var controller = new KbmController(target);

        var enter = new MouseEnterEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller.OnMouseEnter(ref enter);
        Assert.True(target.Hovered.Value);

        var capture = new MouseButtonEvent
        {
            Mouse = new Mouse(), Button = MouseButton.Left, State = InputState.Pressed, Phase = EventPhase.Capturing,
        };
        controller.OnMouseButtonStateChanged(ref capture);
        Assert.Equal(0, target.Activations);
        Assert.False(capture.IsConsumed);

        var press = new MouseButtonEvent
        {
            Mouse = new Mouse(), Button = MouseButton.Left, State = InputState.Pressed, Phase = EventPhase.Bubbling,
        };
        controller.OnMouseButtonStateChanged(ref press);
        Assert.Equal(1, target.Activations);
        Assert.True(target.Pressed.Value);
        Assert.True(press.IsConsumed);

        var release = new MouseButtonEvent
        {
            Mouse = new Mouse(), Button = MouseButton.Left, State = InputState.Released, Phase = EventPhase.Bubbling,
        };
        controller.OnMouseButtonStateChanged(ref release);
        Assert.False(target.Pressed.Value);

        var exit = new MouseExitEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller.OnMouseExit(ref exit);
        Assert.False(target.Hovered.Value);
    }

    [Fact]
    public void Disabled_IgnoresHoverAndActivation()
    {
        var target = new FakeInteractable();
        target.Enabled.Value = false;
        var controller = new KbmController(target);

        var enter = new MouseEnterEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller.OnMouseEnter(ref enter);
        Assert.False(target.Hovered.Value);

        var press = new MouseButtonEvent
        {
            Mouse = new Mouse(), Button = MouseButton.Left, State = InputState.Pressed, Phase = EventPhase.Bubbling,
        };
        controller.OnMouseButtonStateChanged(ref press);
        Assert.Equal(0, target.Activations);
        Assert.False(press.IsConsumed);
    }

    [Fact]
    public void WithController_DiBuildsController_InjectingTarget_AndFollowsMountedLifetime()
    {
        var input = new InputSystem();
        var target = new FakeInteractable();
        var view = new Box()
            .WithController<KbmController>(target)
            .BuildView(ContextWith(input));

        Assert.Null(input.GetController(view));

        view.Mount();
        var controller = input.GetController(view);
        Assert.IsType<KbmController>(controller);

        var enter = new MouseEnterEvent { Mouse = new Mouse(), Phase = EventPhase.Bubbling };
        controller!.OnMouseEnter(ref enter);
        Assert.True(target.Hovered.Value);

        view.Unmount();
        Assert.Null(input.GetController(view));
    }
}
