namespace JpegSharp.Transforms;

/// <summary>
/// Reference 8x8 two-dimensional Discrete Cosine Transform (type II) and its inverse,
/// implemented in the orthonormal (energy-preserving) formulation used by JPEG.
/// </summary>
/// <remarks>
/// <para>
/// The transform is separable: a 2D DCT of an 8x8 block equals <c>A · f · Aᵀ</c> where
/// <c>A</c> is the orthonormal 1D DCT basis matrix. The implementation therefore applies
/// a 1D transform to the rows and then to the columns, giving O(n³) work per block on a
/// tiny fixed 8x8 grid.
/// </para>
/// <para>
/// This is the numerically exact <em>reference</em> transform used to validate the faster
/// integer/AAN fixed-point paths. Because the basis is orthonormal, <see cref="Forward"/>
/// and <see cref="Inverse"/> are exact inverses (to floating-point tolerance) and the DC
/// coefficient of a flat block equals eight times the sample value.
/// </para>
/// </remarks>
internal static class Dct
{
    /// <summary>The side length of a DCT block.</summary>
    public const int N = 8;

    /// <summary>The number of coefficients in a block (<see cref="N"/> × <see cref="N"/>).</summary>
    public const int BlockSize = N * N;

    // Basis[u * N + x] = orthonormal 1D DCT-II coefficient.
    private static readonly double[] Basis = BuildBasis();

    /// <summary>
    /// Applies the forward 2D DCT to a block of spatial samples.
    /// </summary>
    /// <param name="input">64 spatial samples in row-major order.</param>
    /// <param name="output">Receives 64 frequency coefficients in row-major (u,v) order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void Forward(ReadOnlySpan<double> input, Span<double> output)
    {
        EnsureBlock(input, nameof(input));
        EnsureBlock(output, nameof(output));

        Span<double> temp = stackalloc double[BlockSize];

        // Row pass: temp = A · f  (transform each column index x over rows -> u).
        // temp[u][y] = Σ_x Basis[u][x] · input[x][y]
        for (var u = 0; u < N; u++)
        {
            for (var y = 0; y < N; y++)
            {
                double acc = 0;
                for (var x = 0; x < N; x++)
                    acc += Basis[u * N + x] * input[x * N + y];
                temp[u * N + y] = acc;
            }
        }

        // Column pass: output = temp · Aᵀ.
        // output[u][v] = Σ_y temp[u][y] · Basis[v][y]
        for (var u = 0; u < N; u++)
        {
            for (var v = 0; v < N; v++)
            {
                double acc = 0;
                for (var y = 0; y < N; y++)
                    acc += temp[u * N + y] * Basis[v * N + y];
                output[u * N + v] = acc;
            }
        }
    }

    /// <summary>
    /// Applies the inverse 2D DCT to a block of frequency coefficients.
    /// </summary>
    /// <param name="input">64 frequency coefficients in row-major (u,v) order.</param>
    /// <param name="output">Receives 64 reconstructed spatial samples in row-major order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void Inverse(ReadOnlySpan<double> input, Span<double> output)
    {
        EnsureBlock(input, nameof(input));
        EnsureBlock(output, nameof(output));

        Span<double> temp = stackalloc double[BlockSize];

        // f = Aᵀ · F · A.
        // temp[x][v] = Σ_u Basis[u][x] · F[u][v]
        for (var x = 0; x < N; x++)
        {
            for (var v = 0; v < N; v++)
            {
                double acc = 0;
                for (var u = 0; u < N; u++)
                    acc += Basis[u * N + x] * input[u * N + v];
                temp[x * N + v] = acc;
            }
        }

        // output[x][y] = Σ_v temp[x][v] · Basis[v][y]
        for (var x = 0; x < N; x++)
        {
            for (var y = 0; y < N; y++)
            {
                double acc = 0;
                for (var v = 0; v < N; v++)
                    acc += temp[x * N + v] * Basis[v * N + y];
                output[x * N + y] = acc;
            }
        }
    }

    private static double[] BuildBasis()
    {
        var basis = new double[BlockSize];
        var scale0 = Math.Sqrt(1.0 / N);
        var scale = Math.Sqrt(2.0 / N);
        for (var u = 0; u < N; u++)
        {
            var cu = u == 0 ? scale0 : scale;
            for (var x = 0; x < N; x++)
                basis[u * N + x] = cu * Math.Cos((2 * x + 1) * u * Math.PI / (2 * N));
        }
        return basis;
    }

    private static void EnsureBlock<T>(ReadOnlySpan<T> span, string name)
    {
        if (span.Length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} values.", name);
    }
}
