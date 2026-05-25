using ZGF.Observable;

namespace GitGui;

internal sealed class ModeSwitcherViewModel : IDisposable
{
    private readonly State<MainViewMode> _mode;
    private readonly Derived<MainViewMode> _modeView;

    public IReadable<MainViewMode> Mode => _modeView;

    public ModeSwitcherViewModel(State<MainViewMode> mode)
    {
        _mode = mode;
        _modeView = new Derived<MainViewMode>(() => _mode.Value);
    }

    public void Activate(MainViewMode mode)
    {
        _mode.Value = mode;
    }

    public void Dispose()
    {
        _modeView.Dispose();
    }
}
