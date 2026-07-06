namespace ZGF.Svg;

/// <summary>The SVG viewBox in user units. Y grows downward, per the SVG coordinate system.</summary>
public readonly record struct SvgViewBox(float MinX, float MinY, float Width, float Height);
