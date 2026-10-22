namespace ZGF.Gui.Immediate;

public sealed partial class Ui
{
    private static int _slotCount;

    private static class Slot<T>
    {
        public static readonly int Index = Interlocked.Increment(ref _slotCount) - 1;
    }

    private object?[] _context = new object?[8];
    private readonly Stack<(int Slot, object? Previous)> _provided = new();

    /// <summary>Makes <paramref name="value"/> what <see cref="Get{T}"/> returns for everything
    /// recorded inside the scope. This is how a widget library receives its theme or services, and
    /// how a subtree overrides them.</summary>
    public Scope Provide<T>(T value) where T : class
    {
        var slot = Slot<T>.Index;
        if (slot >= _context.Length)
            Array.Resize(ref _context, Math.Max(slot + 1, _context.Length * 2));
        _provided.Push((slot, _context[slot]));
        _context[slot] = value;
        return BeginScope(ScopeKind.Context);
    }

    /// <summary>The innermost value given to <see cref="Provide{T}"/>, or null outside any.</summary>
    public T? Get<T>() where T : class
    {
        var slot = Slot<T>.Index;
        return slot < _context.Length ? (T?)_context[slot] : null;
    }

    private void EndProvide()
    {
        var (slot, previous) = _provided.Pop();
        _context[slot] = previous;
    }

    private void ResetContext()
    {
        _provided.Clear();
        Array.Clear(_context);
    }
}
