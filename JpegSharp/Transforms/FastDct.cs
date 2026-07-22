namespace JpegSharp.Transforms;

/// <summary>
/// A fast, numerically exact 8x8 DCT/IDCT using the even/odd decomposition of the 1D
/// transform. Exploiting the symmetry <c>cos((2(7-x)+1)uπ/16) = (-1)ᵘ cos((2x+1)uπ/16)</c>
/// splits each 8-point 1D transform into two 4-point sums, halving the multiplications
/// relative to a naive matrix multiply while producing identical results (to floating-point
/// tolerance) to the reference <see cref="Dct"/>.
/// </summary>
/// <remarks>
/// This is the transform used on the encode and decode hot paths. The reference
/// <see cref="Dct"/> remains the correctness oracle it is validated against.
/// </remarks>
internal static class FastDct
{
    private const int N = 8;
    private const int BlockSize = N * N;

    // Even[x, j] = A[2j][x], Odd[x, j] = A[2j+1][x], where A is the orthonormal 1D DCT basis.
    private static readonly double[,] Even = new double[4, 4];
    private static readonly double[,] Odd = new double[4, 4];

    static FastDct()
    {
        for (var x = 0; x < 4; x++)
        {
            for (var j = 0; j < 4; j++)
            {
                Even[x, j] = Basis(2 * j, x);
                Odd[x, j] = Basis(2 * j + 1, x);
            }
        }
    }

    /// <summary>Applies the forward 2D DCT to a block of spatial samples.</summary>
    /// <param name="input">64 spatial samples in row-major order.</param>
    /// <param name="output">Receives 64 frequency coefficients in row-major order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void Forward(ReadOnlySpan<double> input, Span<double> output)
    {
        EnsureBlock(input, nameof(input));
        EnsureBlock(output, nameof(output));

        Span<double> temp = stackalloc double[BlockSize];
        Span<double> line = stackalloc double[N];
        Span<double> result = stackalloc double[N];

        // Column pass.
        for (var c = 0; c < N; c++)
        {
            for (var i = 0; i < N; i++)
                line[i] = input[i * N + c];
            Forward1D(line, result);
            for (var i = 0; i < N; i++)
                temp[i * N + c] = result[i];
        }

        // Row pass.
        for (var r = 0; r < N; r++)
        {
            Forward1D(temp.Slice(r * N, N), result);
            for (var i = 0; i < N; i++)
                output[r * N + i] = result[i];
        }
    }

    /// <summary>Applies the inverse 2D DCT to a block of frequency coefficients.</summary>
    /// <param name="input">64 frequency coefficients in row-major order.</param>
    /// <param name="output">Receives 64 reconstructed spatial samples in row-major order.</param>
    /// <exception cref="ArgumentException">A span is not exactly 64 elements long.</exception>
    public static void Inverse(ReadOnlySpan<double> input, Span<double> output)
    {
        EnsureBlock(input, nameof(input));
        EnsureBlock(output, nameof(output));

        Span<double> temp = stackalloc double[BlockSize];
        Span<double> line = stackalloc double[N];
        Span<double> result = stackalloc double[N];

        // Column pass.
        for (var c = 0; c < N; c++)
        {
            for (var i = 0; i < N; i++)
                line[i] = input[i * N + c];
            Inverse1D(line, result);
            for (var i = 0; i < N; i++)
                temp[i * N + c] = result[i];
        }

        // Row pass.
        for (var r = 0; r < N; r++)
        {
            Inverse1D(temp.Slice(r * N, N), result);
            for (var i = 0; i < N; i++)
                output[r * N + i] = result[i];
        }
    }

    private static void Forward1D(ReadOnlySpan<double> f, Span<double> output)
    {
        // s[x] = f[x] + f[7-x], d[x] = f[x] - f[7-x] for x in 0..3.
        double s0 = f[0] + f[7], s1 = f[1] + f[6], s2 = f[2] + f[5], s3 = f[3] + f[4];
        double d0 = f[0] - f[7], d1 = f[1] - f[6], d2 = f[2] - f[5], d3 = f[3] - f[4];

        for (var j = 0; j < 4; j++)
        {
            output[2 * j] = Even[0, j] * s0 + Even[1, j] * s1 + Even[2, j] * s2 + Even[3, j] * s3;
            output[2 * j + 1] = Odd[0, j] * d0 + Odd[1, j] * d1 + Odd[2, j] * d2 + Odd[3, j] * d3;
        }
    }

    private static void Inverse1D(ReadOnlySpan<double> freq, Span<double> output)
    {
        // E[x] = sum_j Even[x,j]*F[2j], O[x] = sum_j Odd[x,j]*F[2j+1].
        double e0 = freq[0], e2 = freq[2], e4 = freq[4], e6 = freq[6];
        double o1 = freq[1], o3 = freq[3], o5 = freq[5], o7 = freq[7];

        for (var x = 0; x < 4; x++)
        {
            var even = Even[x, 0] * e0 + Even[x, 1] * e2 + Even[x, 2] * e4 + Even[x, 3] * e6;
            var odd = Odd[x, 0] * o1 + Odd[x, 1] * o3 + Odd[x, 2] * o5 + Odd[x, 3] * o7;
            output[x] = even + odd;
            output[7 - x] = even - odd;
        }
    }

    private static double Basis(int u, int x)
    {
        var scale = u == 0 ? Math.Sqrt(1.0 / N) : Math.Sqrt(2.0 / N);
        return scale * Math.Cos((2 * x + 1) * u * Math.PI / (2 * N));
    }

    private static void EnsureBlock<T>(ReadOnlySpan<T> span, string name)
    {
        if (span.Length != BlockSize)
            throw new ArgumentException($"Block must contain exactly {BlockSize} values.", name);
    }
}
