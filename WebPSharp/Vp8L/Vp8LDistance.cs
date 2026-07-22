using WebPSharp.Api.Exceptions;

namespace WebPSharp.Vp8L;

/// <summary>
/// Maps a VP8L distance "plane code" to an actual pixel back-reference distance. Distances beyond
/// the near neighborhood are encoded directly as <c>planeCode - 120</c>; the first 120 plane codes
/// address a small 2D neighborhood via a reference table.
/// </summary>
internal static class Vp8LDistance
{
    /// <summary>The number of plane codes that address the near 2D neighborhood.</summary>
    public const int NearDistanceCodes = 120;

    /// <summary>Converts a plane code to a pixel distance for an image of the given row width.</summary>
    /// <param name="xSize">The image width in pixels (row stride in the ARGB plane).</param>
    /// <param name="planeCode">The distance plane code (≥ 1).</param>
    /// <returns>The back-reference distance in pixels (≥ 1).</returns>
    /// <exception cref="WebPCorruptException">
    /// A near-neighborhood plane code (≤ 120) is encountered; small-distance decoding is pending
    /// reference-table validation (tracked in the checklist).
    /// </exception>
    public static int PlaneCodeToDistance(int xSize, int planeCode)
    {
        if (planeCode > NearDistanceCodes)
            return planeCode - NearDistanceCodes;

        throw new WebPCorruptException(
            $"VP8L near-distance plane code {planeCode} is not yet supported (pending reference-table validation).");
    }
}
