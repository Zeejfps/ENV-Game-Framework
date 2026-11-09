namespace ZGF.Gui;

/// <summary>
/// Which textures the canvases sharing one set of images still hold, and the textures those images no longer
/// name. A canvas holds each texture its last frame staged until it begins another frame or drops that one, since
/// it may draw the frame again; a texture retired while held is freed when its last hold goes, and at once if
/// nothing holds it.
/// </summary>
public sealed class HeldTextures<T>
{
    private readonly Action<T> _free;
    private readonly Dictionary<uint, int> _holds = new();
    private readonly Dictionary<uint, T> _retired = new();
    private bool _freedAll;

    /// <param name="free">Frees a retired texture, on the thread that owns the textures.</param>
    public HeldTextures(Action<T> free)
    {
        _free = free;
    }

    /// <summary>How many retired textures a canvas frame still holds.</summary>
    public int RetiredCount => _retired.Count;

    public void Hold(uint textureId)
    {
        if (_freedAll) return;
        _holds.TryGetValue(textureId, out var count);
        _holds[textureId] = count + 1;
    }

    public void Release(uint textureId)
    {
        if (_freedAll) return;
        if (!_holds.TryGetValue(textureId, out var count))
            throw new InvalidOperationException($"Texture {textureId} is released more often than it was held.");
        if (count > 1)
        {
            _holds[textureId] = count - 1;
            return;
        }
        _holds.Remove(textureId);
        if (_retired.Remove(textureId, out var texture))
            _free(texture);
    }

    /// <summary>Frees <paramref name="texture"/>, known to canvases as <paramref name="textureId"/>, once no canvas
    /// frame holds it.</summary>
    public void Retire(uint textureId, T texture)
    {
        if (_holds.ContainsKey(textureId))
        {
            _retired.Add(textureId, texture);
            return;
        }
        _free(texture);
    }

    /// <summary>Frees every retired texture, as their owner goes away; holds and releases after it change nothing.</summary>
    public void FreeAll()
    {
        _freedAll = true;
        foreach (var texture in _retired.Values)
            _free(texture);
        _retired.Clear();
        _holds.Clear();
    }
}
