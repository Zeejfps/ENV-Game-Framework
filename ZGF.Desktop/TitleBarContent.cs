using ZGF.Geometry;

namespace ZGF.Desktop;

/// <summary>What the app draws in a title bar its content reaches under: the title bar's color, which
/// the platform shows behind its caption buttons, and the areas, in window coordinates, that move the
/// window when dragged and maximize it when double-clicked.</summary>
public sealed record TitleBarContent(uint Background, IReadOnlyList<RectI> DragAreas);
