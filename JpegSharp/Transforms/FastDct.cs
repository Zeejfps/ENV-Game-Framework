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

    // E{x}{j} = A[2j][x], O{x}{j} = A[2j+1][x], where A is the orthonormal 1D DCT basis.
    private static readonly double E00, E01, E02, E03;
    private static readonly double E10, E11, E12, E13;
    private static readonly double E20, E21, E22, E23;
    private static readonly double E30, E31, E32, E33;
    private static readonly double O00, O01, O02, O03;
    private static readonly double O10, O11, O12, O13;
    private static readonly double O20, O21, O22, O23;
    private static readonly double O30, O31, O32, O33;

    static FastDct()
    {
        E00 = Basis(0, 0); E01 = Basis(2, 0); E02 = Basis(4, 0); E03 = Basis(6, 0);
        E10 = Basis(0, 1); E11 = Basis(2, 1); E12 = Basis(4, 1); E13 = Basis(6, 1);
        E20 = Basis(0, 2); E21 = Basis(2, 2); E22 = Basis(4, 2); E23 = Basis(6, 2);
        E30 = Basis(0, 3); E31 = Basis(2, 3); E32 = Basis(4, 3); E33 = Basis(6, 3);
        O00 = Basis(1, 0); O01 = Basis(3, 0); O02 = Basis(5, 0); O03 = Basis(7, 0);
        O10 = Basis(1, 1); O11 = Basis(3, 1); O12 = Basis(5, 1); O13 = Basis(7, 1);
        O20 = Basis(1, 2); O21 = Basis(3, 2); O22 = Basis(5, 2); O23 = Basis(7, 2);
        O30 = Basis(1, 3); O31 = Basis(3, 3); O32 = Basis(5, 3); O33 = Basis(7, 3);
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

        output[0] = E00 * s0 + E10 * s1 + E20 * s2 + E30 * s3;
        output[1] = O00 * d0 + O10 * d1 + O20 * d2 + O30 * d3;
        output[2] = E01 * s0 + E11 * s1 + E21 * s2 + E31 * s3;
        output[3] = O01 * d0 + O11 * d1 + O21 * d2 + O31 * d3;
        output[4] = E02 * s0 + E12 * s1 + E22 * s2 + E32 * s3;
        output[5] = O02 * d0 + O12 * d1 + O22 * d2 + O32 * d3;
        output[6] = E03 * s0 + E13 * s1 + E23 * s2 + E33 * s3;
        output[7] = O03 * d0 + O13 * d1 + O23 * d2 + O33 * d3;
    }

    private static void Inverse1D(ReadOnlySpan<double> freq, Span<double> output)
    {
        // E[x] = sum_j Even[x,j]*F[2j], O[x] = sum_j Odd[x,j]*F[2j+1].
        double e0 = freq[0], e2 = freq[2], e4 = freq[4], e6 = freq[6];
        double o1 = freq[1], o3 = freq[3], o5 = freq[5], o7 = freq[7];

        var even0 = E00 * e0 + E01 * e2 + E02 * e4 + E03 * e6;
        var odd0 = O00 * o1 + O01 * o3 + O02 * o5 + O03 * o7;
        output[0] = even0 + odd0;
        output[7] = even0 - odd0;

        var even1 = E10 * e0 + E11 * e2 + E12 * e4 + E13 * e6;
        var odd1 = O10 * o1 + O11 * o3 + O12 * o5 + O13 * o7;
        output[1] = even1 + odd1;
        output[6] = even1 - odd1;

        var even2 = E20 * e0 + E21 * e2 + E22 * e4 + E23 * e6;
        var odd2 = O20 * o1 + O21 * o3 + O22 * o5 + O23 * o7;
        output[2] = even2 + odd2;
        output[5] = even2 - odd2;

        var even3 = E30 * e0 + E31 * e2 + E32 * e4 + E33 * e6;
        var odd3 = O30 * o1 + O31 * o3 + O32 * o5 + O33 * o7;
        output[3] = even3 + odd3;
        output[4] = even3 - odd3;
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
