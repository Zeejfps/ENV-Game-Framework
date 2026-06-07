using System;
using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.Mobile.Controllers;

/// <summary>
/// View behavior that binds an <see cref="IPointerController"/> to its view's lifetime,
/// registering it with the <see cref="MobileInputSystem"/> when the view joins a context and
/// unregistering on detach. The mobile parallel to ControllerBehavior.
/// </summary>
public sealed class MobilePointerControllerBehavior<T> : IViewBehavior where T : IPointerController
{
    private readonly Func<Context, T> _factory;
    private readonly PointerPhaseFilter _phaseFilter;
    private T? _controller;

    public MobilePointerControllerBehavior(Func<Context, T> factory, PointerPhaseFilter phaseFilter = PointerPhaseFilter.Both)
    {
        _factory = factory;
        _phaseFilter = phaseFilter;
    }

    public void AttachToContext(View view, Context context)
    {
        _controller = _factory(context);
        context.Get<MobileInputSystem>()!.RegisterController(view, _controller, _phaseFilter);
    }

    public void DetachFromContext(View view, Context context)
    {
        if (_controller != null)
            context.Get<MobileInputSystem>()?.UnregisterController(view, _controller);
        if (_controller is IDisposable disposable)
            disposable.Dispose();
        _controller = default;
    }
}
