namespace ZGF.Gui;

public sealed class InputControllerBehavior : IViewBehavior
{
    private readonly Func<View, Context, IKeyboardMouseController> _factory;
    private readonly EventPhaseFilter _phaseFilter;

    public InputControllerBehavior(IKeyboardMouseController controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
        : this((_, _) => controller, phaseFilter)
    {
    }

    public InputControllerBehavior(Func<View, Context, IKeyboardMouseController> factory, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _factory = factory;
        _phaseFilter = phaseFilter;
    }

    public void OnAttachedToContext(View view, Context context)
    {
        var controller = _factory(view, context);
        context.Get<InputSystem>()!.RegisterController(view, controller, _phaseFilter);
    }

    public void OnDetachedFromContext(View view, Context context)
    {
        context.Get<InputSystem>()?.UnregisterController(view);
    }
}
