namespace ZGF.Gui.Immediate;

/// <summary>Marks a partial type whose <c>uint</c> color properties, and the colors of its
/// <see cref="UiShadow"/> properties, a theme file can set. A generator gives the type a static
/// <c>ColorKeys</c> list, a static <c>FromColors</c> that builds it from colors by key, and
/// <c>ToColors</c>, which lists an instance's colors by key.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ThemeColorsAttribute : Attribute;

/// <summary>A color a theme file names by <see cref="Key"/>, the property's name in camelCase, or the
/// shadow property's name followed by <c>Color</c>. A color the type does not derive from others
/// <see cref="IsRequired"/>. <see cref="Description"/> is the property's summary as plain text, or empty.</summary>
public readonly record struct ThemeColorKey(string Key, bool IsRequired, string Description);
