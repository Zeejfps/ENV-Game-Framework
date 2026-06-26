namespace ZGF.Gui;

/// <summary>
/// Per-frame animation callbacks. A registered tick runs once per UI tick with the elapsed
/// seconds; an animation advances its state there, and the resulting <c>SetDirty</c> schedules
/// the next frame — frames self-sustain while the animation runs and stop when it unregisters.
/// </summary>
public interface IFrameTicker
{
    void Add(Action<float> tick);
    void Remove(Action<float> tick);
}

/// <summary>
/// The app-owned <see cref="IFrameTicker"/>. Ticks are invoked from a snapshot, so adding or
/// removing during a tick takes effect next frame.
/// </summary>
public sealed class FrameTicker : IFrameTicker
{
    private readonly List<Action<float>> _ticks = new();
    private readonly Action? _onActivated;
    private Action<float>[] _buffer = new Action<float>[4];

    public FrameTicker(Action? onActivated = null)
    {
        _onActivated = onActivated;
    }

    /// <summary>Number of registered ticks. Zero means no animation is driving the loop — the UI
    /// is at rest. A test harness ticks until this reaches zero to reach a settled frame.</summary>
    public int ActiveCount => _ticks.Count;

    public void Add(Action<float> tick)
    {
        ArgumentNullException.ThrowIfNull(tick);
        _ticks.Add(tick);
        _onActivated?.Invoke();
    }

    public void Remove(Action<float> tick)
    {
        _ticks.Remove(tick);
    }

    public void Tick(float dtSeconds)
    {
        var count = _ticks.Count;
        if (count == 0) return;

        if (_buffer.Length < count)
            _buffer = new Action<float>[Math.Max(count, _buffer.Length * 2)];
        _ticks.CopyTo(_buffer);

        for (var i = 0; i < count; i++)
            _buffer[i](dtSeconds);

        Array.Clear(_buffer, 0, count);
    }
}
