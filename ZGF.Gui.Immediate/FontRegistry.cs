using ZGF.Fonts;

namespace ZGF.Gui.Immediate;

/// <summary>The fonts registered with a canvas, by family. Each face is loaded once for its bytes, so
/// registering unchanged bytes again, as reading an unchanged file again gives, reuses it. A face no
/// family draws with any more, as when its family is registered again with changed bytes or is
/// unregistered, is released by <see cref="ReleaseUnused"/>, which runs between frames so that a frame
/// drawing with a face never loses it halfway.</summary>
internal sealed class FontRegistry(FreeTypeFontBackend fonts, RenderedCanvasBase canvas, Func<byte[], int, FontHandle> load)
{
    private readonly Dictionary<FontData, FontHandle> _loaded = new();
    private readonly Dictionary<(string Family, bool Bold), FontHandle> _registered = new();
    private readonly List<FontHandle> _superseded = new();

    /// <summary>How many faces are loaded and not released.</summary>
    public int LoadedCount => _loaded.Count;

    /// <summary>Draws <paramref name="font"/>'s family with it, in place of the face it was drawn with
    /// before, which is released once no family draws with it.</summary>
    public void Register(FontFace font)
    {
        var handle = LoadOnce(font);
        var slot = (font.Family, font.Bold);
        Replace(slot, handle);
        if (font.Bold)
            canvas.RegisterBoldFont(font.Family, handle);
        else
            canvas.RegisterFont(font.Family, handle);
    }

    private FontHandle LoadOnce(FontFace font)
    {
        var key = new FontData(font.Data, font.FaceIndex);
        if (_loaded.TryGetValue(key, out var loaded))
            return loaded;
        var handle = load(font.Data, font.FaceIndex);
        _loaded[key] = handle;
        return handle;
    }

    private void Replace((string Family, bool Bold) slot, FontHandle handle)
    {
        if (_registered.TryGetValue(slot, out var previous) && previous != handle)
            _superseded.Add(previous);
        _registered[slot] = handle;
    }

    /// <summary>Draws <paramref name="family"/> in the default font again, releasing its faces once no
    /// family draws with them.</summary>
    public void Unregister(string family)
    {
        Remove((family, false));
        Remove((family, true));
        canvas.UnregisterFont(family);
    }

    private void Remove((string Family, bool Bold) slot)
    {
        if (_registered.Remove(slot, out var previous))
            _superseded.Add(previous);
    }

    /// <summary>Releases the faces replaced or unregistered since the last call that no family draws
    /// with any more. Call it between frames.</summary>
    public void ReleaseUnused()
    {
        foreach (var handle in _superseded)
            ReleaseIfUnused(handle);
        _superseded.Clear();
    }

    private void ReleaseIfUnused(FontHandle handle)
    {
        if (_registered.ContainsValue(handle))
            return;
        if (!ForgetLoaded(handle))
            return;
        fonts.ReleaseFont(handle);
    }

    /// <summary>Forgets the bytes <paramref name="handle"/> was loaded from; false when it was already
    /// forgotten and released.</summary>
    private bool ForgetLoaded(FontHandle handle)
    {
        foreach (var (key, loaded) in _loaded)
        {
            if (loaded != handle)
                continue;
            _loaded.Remove(key);
            return true;
        }
        return false;
    }

    /// <summary>A face of font bytes, equal to another of the same bytes, hashed once.</summary>
    private readonly struct FontData : IEquatable<FontData>
    {
        public FontData(byte[] bytes, int faceIndex)
        {
            Bytes = bytes;
            FaceIndex = faceIndex;
            Hash = HashOf(bytes, faceIndex);
        }

        public byte[] Bytes { get; }

        public int FaceIndex { get; }

        private int Hash { get; }

        public bool Equals(FontData other)
        {
            if (FaceIndex != other.FaceIndex)
                return false;
            return Bytes.AsSpan().SequenceEqual(other.Bytes);
        }

        public override bool Equals(object? obj) => obj is FontData other && Equals(other);

        public override int GetHashCode() => Hash;

        private static int HashOf(byte[] bytes, int faceIndex)
        {
            var hash = new HashCode();
            hash.Add(faceIndex);
            hash.AddBytes(bytes);
            return hash.ToHashCode();
        }
    }
}
