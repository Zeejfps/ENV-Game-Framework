using ZGF.Fonts;

namespace ZGF.Gui.Immediate;

/// <summary>The fonts loaded so far, by their data and face. Registering a font whose bytes were loaded
/// before, even from another array, as reading an unchanged file again gives, reuses the face loaded
/// then rather than loading another; bytes that changed load a face of their own.</summary>
internal sealed class LoadedFonts
{
    private readonly Dictionary<FontData, FontHandle> _handles = new();

    /// <summary>How many faces were loaded.</summary>
    public int Count => _handles.Count;

    /// <summary>The face of <paramref name="font"/>, loaded with <paramref name="load"/> the first time
    /// its bytes and face are seen.</summary>
    public FontHandle LoadOnce(FontFace font, Func<byte[], int, FontHandle> load)
    {
        var key = new FontData(font.Data, font.FaceIndex);
        if (_handles.TryGetValue(key, out var loaded))
            return loaded;
        var handle = load(font.Data, font.FaceIndex);
        _handles[key] = handle;
        return handle;
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
