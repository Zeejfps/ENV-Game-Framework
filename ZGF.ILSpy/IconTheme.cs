using ZGF.Gui.Immediate;

namespace ZGF.ILSpy;

/// <summary>An icon theme as resolved over the themes it extends: what each key draws, the fonts its
/// glyphs are drawn in, and each key's icon as a theme file writes it, the reference theme's where no
/// theme in the chain sets the key.</summary>
public sealed class IconTheme(IconSet set, IReadOnlyList<FontFace> fonts, IReadOnlyDictionary<string, IconValue> values)
{
    /// <summary>What each key draws.</summary>
    public AppIcons Icons { get; } = new(set);

    /// <summary>The icons by key, falling back to the reference theme's.</summary>
    public IconSet Set => set;

    /// <summary>The fonts the glyphs are drawn in, which the screen must have registered.</summary>
    public IReadOnlyList<FontFace> Fonts => fonts;

    /// <summary>Each key's icon as a theme file writes it.</summary>
    public IReadOnlyDictionary<string, IconValue> Values => values;
}
