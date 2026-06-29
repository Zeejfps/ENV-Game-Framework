namespace ZGF.Gui.Desktop.Components.DataGrid;

/// <summary>
/// One user-draggable length (a column width): the single source of truth for a resizable boundary. Holds
/// the current value clamped to <c>[Min, Max]</c> and raises <see cref="Changed"/> only on a real change, so
/// every resizable in a grid shares one clamp/notify implementation.
/// </summary>
public sealed class ResizableExtent
{
    private float _value;

    public ResizableExtent(float value, float min, float max)
    {
        Min = min;
        Max = max;
        _value = Math.Clamp(value, min, Math.Max(min, max));
    }

    public float Min { get; }
    public float Max { get; }
    public float Value => _value;

    /// <summary>Raised whenever <see cref="Value"/> actually changes.</summary>
    public event Action? Changed;

    /// <summary>Sets the value to the clamped <paramref name="raw"/>, returning whether it moved.
    /// <paramref name="max"/> overrides the ceiling for this call — a boundary may clamp against a live
    /// neighbour rather than its fixed maximum — while the floor stays <see cref="Min"/>.</summary>
    public bool Set(float raw, float? max = null)
    {
        var next = Math.Clamp(raw, Min, Math.Max(Min, max ?? Max));
        if (next == _value) return false;
        _value = next;
        Changed?.Invoke();
        return true;
    }
}
