using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

public sealed class ControllerBehavior<T> : IViewBehavior where T : IKeyboardMouseController
{
    private readonly Func<Context, T>? _factory;
    private readonly EventPhaseFilter _phaseFilter;
    private readonly bool _ownsController;
    private T? _controller;

    public ControllerBehavior(Func<Context, T> factory, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _factory = factory;
        _phaseFilter = phaseFilter;
        _ownsController = true;
    }

    public ControllerBehavior(T controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _controller = controller;
        _phaseFilter = phaseFilter;
        _ownsController = false;
    }

    public void AttachToContext(View view, Context context)
    {
        _controller ??= _factory!(context);
        context.Get<InputSystem>()!.RegisterController(view, _controller, _phaseFilter);
    }

    public void DetachFromContext(View view, Context context)
    {
        if (_controller != null)
            context.Get<InputSystem>()?.UnregisterController(view, _controller);

        if (_ownsController)
        {
            if (_controller is IDisposable disposable)
                disposable.Dispose();
            _controller = default;
        }
    }
}
