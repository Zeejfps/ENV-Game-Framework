namespace ZGF.Fonts;

public readonly record struct FontFeature(uint Tag, uint Value)
{
    public static uint MakeTag(char a, char b, char c, char d)
        => ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | (uint)d;
}

public readonly struct FontFeatureSet : IEquatable<FontFeatureSet>
{
    private const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    private readonly FontFeature[]? _features;

    public FontFeatureSet(params FontFeature[]? features)
    {
        if (features is null || features.Length == 0)
        {
            _features = null;
            Signature = 0;
            return;
        }

        _features = features;

        var sig = FnvOffset;
        foreach (var f in features)
        {
            sig = (sig ^ f.Tag) * FnvPrime;
            sig = (sig ^ f.Value) * FnvPrime;
        }
        Signature = sig == 0 ? 1 : sig;
    }

    public static FontFeatureSet None => default;

    public static readonly FontFeatureSet TabularFigures =
        new(new FontFeature(FontFeature.MakeTag('t', 'n', 'u', 'm'), 1));

    public ulong Signature { get; }

    public bool IsEmpty => Signature == 0;

    public ReadOnlySpan<FontFeature> Features => _features;

    public bool Equals(FontFeatureSet other) => Signature == other.Signature;
    public override bool Equals(object? obj) => obj is FontFeatureSet o && Equals(o);
    public override int GetHashCode() => Signature.GetHashCode();
    public static bool operator ==(FontFeatureSet a, FontFeatureSet b) => a.Signature == b.Signature;
    public static bool operator !=(FontFeatureSet a, FontFeatureSet b) => !(a == b);
}
