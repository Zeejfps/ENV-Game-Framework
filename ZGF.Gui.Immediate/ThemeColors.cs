namespace ZGF.Gui.Immediate;

/// <summary>Marks a partial type whose <c>uint</c> color properties, and the colors of its
/// <see cref="UiShadow"/> properties, a theme file can set. A generator gives the type a static
/// <c>ColorKeys</c> list and a static <c>FromColors</c> that builds it from colors by key.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ThemeColorsAttribute : Attribute;

/// <summary>A color a theme file names by <see cref="Key"/>, the property's name in camelCase, or the
/// shadow property's name followed by <c>Color</c>. A color the type does not derive from others
/// <see cref="IsRequired"/>.</summary>
public readonly record struct ThemeColorKey(string Key, bool IsRequired);
