namespace ZGF.Gui.Hosting;

/// <summary>Data too large for one X request, sent to a requestor's property a chunk at a time,
/// each once the requestor has deleted the one before (the ICCCM INCR protocol).</summary>
internal sealed class IncrementalTransfer(uint requestor, uint property, uint type, byte[] data, long now)
{
    // A requestor that stops reading (it quit, or hung) is given up on after this long.
    private const long StallMilliseconds = 5000;

    private int _sent;

    public uint Requestor { get; } = requestor;
    public uint Property { get; } = property;
    public uint Type { get; } = type;
    public int Length => data.Length;
    public bool IsDone { get; private set; }
    private long LastActivity { get; set; } = now;

    public bool Feeds(uint window, uint atom) => window == Requestor && atom == Property;

    public bool IsStalled(long now) => now - LastActivity > StallMilliseconds;

    /// <summary>The next chunk, at most <paramref name="maxLength"/> bytes; empty once all is sent,
    /// which ends the transfer.</summary>
    public ReadOnlyMemory<byte> TakeChunk(int maxLength, long now)
    {
        LastActivity = now;
        var length = Math.Min(maxLength, data.Length - _sent);
        var chunk = new ReadOnlyMemory<byte>(data, _sent, length);
        _sent += length;
        if (length == 0)
            IsDone = true;
        return chunk;
    }
}
