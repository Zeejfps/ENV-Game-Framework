namespace ZGF.Fonts;

public readonly struct FontHandle : IEquatable<FontHandle>
{
    public readonly int Id;
    public FontHandle(int id) => Id = id;

    public bool IsValid => Id > 0;

    public bool Equals(FontHandle other) => Id == other.Id;
    public override bool Equals(object? obj) => obj is FontHandle h && Equals(h);
    public override int GetHashCode() => Id;
    public static bool operator ==(FontHandle a, FontHandle b) => a.Id == b.Id;
    public static bool operator !=(FontHandle a, FontHandle b) => a.Id != b.Id;
}
