namespace ZGF.Gui;

/// <summary>
/// The box one cell of a monospaced grid occupies, in logical points.
/// </summary>
/// <remarks>
/// Both dimensions are whole device pixels, so that N cells measure exactly N times one cell. A
/// fractional advance accumulates: a quarter pixel per column is twenty-five pixels of drift across
/// a hundred columns, and box-drawing characters stop meeting long before that.
/// </remarks>
public readonly record struct CellMetrics(float Advance, float Height);
