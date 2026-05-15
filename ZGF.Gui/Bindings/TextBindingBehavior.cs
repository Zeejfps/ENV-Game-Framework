using ZGF.Observable;

namespace ZGF.Gui.Bindings;

internal sealed class TextBindingBehavior<T> : IViewBehavior
{
    private readonly IReadable<T> _source;
    private readonly Func<T, string?> _format;
    private IDisposable? _subscription;
    private TextView? _view;

    public TextBindingBehavior(IReadable<T> source, Func<T, string?> format)
    {
        _source = source;
        _format = format;
    }

    public void AttachToContext(View view, Context context)
    {
        _view = (TextView)view;
        _subscription = _source.Subscribe(OnSourceChanged);
    }

    public void DetachFromContext(View view, Context context)
    {
        _subscription?.Dispose();
        _subscription = null;
        _view = null;
    }

    private void OnSourceChanged(T value)
    {
        if (_view != null)
        {
            _view.Text = _format(value);
        }
    }
}
