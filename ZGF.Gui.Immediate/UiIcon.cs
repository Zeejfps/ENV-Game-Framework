namespace ZGF.Gui.Immediate;

/// <summary>A glyph shown as an icon. A color of 0 takes the theme's muted text color. A nonzero
/// <see cref="Background"/> draws it on a rounded badge of that color. <see cref="Overlay"/> is a small
/// glyph over its bottom-right corner — a lock on a private member.</summary>
public readonly record struct UiIcon(IconGlyph Glyph, uint Color = 0, uint Background = 0, IconGlyph? Overlay = null, uint OverlayColor = 0);
