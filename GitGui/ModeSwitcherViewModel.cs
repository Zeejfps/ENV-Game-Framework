using ZGF.Observable;

namespace GitGui;

internal sealed class SegmentViewModel : IDisposable
{
    private readonly State<bool> _isActive = new(false);
    private readonly Action _onClick;

    public IReadable<bool> IsActive => _isActive;

    public SegmentViewModel(Action onClick)
    {
        _onClick = onClick;
    }

    public void Click() => _onClick();

    internal void SetActive(bool active) => _isActive.Value = active;

    public void Dispose() => _isActive.Dispose();
}

internal sealed class ModeSwitcherViewModel : IDisposable
{
    private readonly IDisposable _modeSubscription;

    public SegmentViewModel HistorySegment { get; }
    public SegmentViewModel LocalChangesSegment { get; }

    public ModeSwitcherViewModel(State<MainViewMode> mode)
    {
        HistorySegment      = new SegmentViewModel(() => mode.Value = MainViewMode.History);
        LocalChangesSegment = new SegmentViewModel(() => mode.Value = MainViewMode.LocalChanges);

        _modeSubscription = mode.Subscribe(m =>
        {
            HistorySegment.SetActive(m == MainViewMode.History);
            LocalChangesSegment.SetActive(m == MainViewMode.LocalChanges);
        });
    }

    public void Dispose()
    {
        _modeSubscription.Dispose();
        HistorySegment.Dispose();
        LocalChangesSegment.Dispose();
    }
}
