using ZGF.Geometry;

namespace ZGF.Gui;

/// <summary>
/// A run of monospaced cells sharing one style: consecutive columns, one code point each.
/// </summary>
/// <remarks>
/// There is no width and no cell height here, because both are already implied — the run ends after
/// <c>CodePoints.Length</c> columns of <see cref="CellAdvance"/>, and the vertical placement needs
/// only the baseline, which comes from the font. A caller cannot describe a run whose stated size
/// disagrees with the run it actually drew.
/// </remarks>
public readonly ref struct DrawGlyphRunInputs
{
    /// <summary>Top-left corner of the first cell.</summary>
    public required PointF Origin { get; init; }

    /// <summary>One code point per column, in column order.</summary>
    public required ReadOnlySpan<int> CodePoints { get; init; }

    /// <summary>The column pitch in logical points — see <see cref="CellMetrics.Advance"/>.</summary>
    public required float CellAdvance { get; init; }

    public required TextStyle Style { get; init; }

    public required int ZIndex { get; init; }

    public bool Underline { get; init; }

    public bool StrikeThrough { get; init; }
}
