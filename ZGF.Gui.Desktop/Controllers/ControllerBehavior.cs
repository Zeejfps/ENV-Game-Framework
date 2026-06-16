using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

public sealed class ControllerBehavior<T> : IViewBehavior where T : IKeyboardMouseController
{
    private readonly InputSystem _input;
    private readonly Func<T>? _factory;
    private readonly EventPhaseFilter _phaseFilter;
    private readonly bool _ownsController;
    private T? _controller;

    public ControllerBehavior(InputSystem input, Func<T> factory, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _input = input;
        _factory = factory;
        _phaseFilter = phaseFilter;
        _ownsController = true;
    }

    public ControllerBehavior(InputSystem input, T controller, EventPhaseFilter phaseFilter = EventPhaseFilter.Both)
    {
        _input = input;
        _controller = controller;
        _phaseFilter = phaseFilter;
        _ownsController = false;
    }

    public void Attach(View view)
    {
        _controller ??= _factory!();
        _input.RegisterController(view, _controller, _phaseFilter);
    }

    public void Detach(View view)
    {
        if (_controller != null)
            _input.UnregisterController(view, _controller);

        if (_ownsController)
        {
            if (_controller is IDisposable disposable)
                disposable.Dispose();
            _controller = default;
        }
    }
}
