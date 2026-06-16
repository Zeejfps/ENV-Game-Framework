using ZGF.Observable;

namespace ZGF.Gui.Bindings;

internal sealed class TwoWayStateBindingBehavior<T> : IViewBehavior
{
    private readonly State<T> _target;
    private readonly State<T> _source;
    private IDisposable? _binding;

    public TwoWayStateBindingBehavior(State<T> target, State<T> source)
    {
        _target = target;
        _source = source;
    }

    public void Attach(View view)
    {
        _binding = _target.BindTwoWay(_source);
    }

    public void Detach(View view)
    {
        _binding?.Dispose();
        _binding = null;
    }
}
