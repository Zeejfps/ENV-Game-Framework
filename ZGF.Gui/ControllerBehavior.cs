using ZGF.Gui.Desktop;

namespace ZGF.Gui;

public sealed class ControllerBehavior<T> : IViewBehavior where T : IKeyboardMouseController
{
    private readonly Func<Context, T> _factory;
    private readonly EventPhaseFilter _phaseFilter;
    private T? _controller;

    public ControllerBehavior(Func<Context, T> factory, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _factory = factory;
        _phaseFilter = phaseFilter;
    }

    public void AttachToContext(View view, Context context)
    {
        _controller = _factory(context);
        context.Get<InputSystem>()!.RegisterController(view, _controller, _phaseFilter);
    }

    public void DetachFromContext(View view, Context context)
    {
        context.Get<InputSystem>()?.UnregisterController(view);
        if (_controller is IDisposable disposable)
            disposable.Dispose();
        _controller = default;
    }
}
