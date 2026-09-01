using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8;

/// <summary>
/// The VP8 block transforms: the 4x4 inverse and forward DCT-like transform used for residuals, and
/// the 4x4 Walsh-Hadamard transform used for the luma DC coefficients of a 16x16 macroblock. All are
/// the exact fixed-point integer transforms defined by RFC 6386. Blocks are 16-element arrays in
/// raster order.
/// </summary>
internal static class Vp8Transform
{
    // Fixed-point cosine/sine multipliers (Q16) for the inverse DCT.
    private const int C1 = 20091; // sqrt(2)*cos(pi/8) - 1
    private const int C2 = 35468; // sqrt(2)*sin(pi/8)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Mul1(int a) => a + ((a * C1) >> 16);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Mul2(int a) => (a * C2) >> 16;

    /// <summary>Applies the inverse 4x4 DCT, producing residual values (before adding to prediction).</summary>
    /// <param name="coeffs">The 16 dequantized coefficients in raster order.</param>
    /// <param name="residual">Receives the 16 residual values in raster order.</param>
    public static void InverseDct(ReadOnlySpan<short> coeffs, Span<int> residual)
    {
        Span<int> tmp = stackalloc int[16];

        for (var i = 0; i < 4; i++)
        {
            var a = coeffs[i] + coeffs[8 + i];
            var b = coeffs[i] - coeffs[8 + i];
            var c = Mul2(coeffs[4 + i]) - Mul1(coeffs[12 + i]);
            var d = Mul1(coeffs[4 + i]) + Mul2(coeffs[12 + i]);
            tmp[4 * i + 0] = a + d;
            tmp[4 * i + 1] = b + c;
            tmp[4 * i + 2] = b - c;
            tmp[4 * i + 3] = a - d;
        }

        for (var i = 0; i < 4; i++)
        {
            var dc = tmp[i] + 4;
            var a = dc + tmp[8 + i];
            var b = dc - tmp[8 + i];
            var c = Mul2(tmp[4 + i]) - Mul1(tmp[12 + i]);
            var d = Mul1(tmp[4 + i]) + Mul2(tmp[12 + i]);
            residual[4 * i + 0] = (a + d) >> 3;
            residual[4 * i + 1] = (b + c) >> 3;
            residual[4 * i + 2] = (b - c) >> 3;
            residual[4 * i + 3] = (a - d) >> 3;
        }
    }

    /// <summary>Applies the forward 4x4 DCT to a residual block.</summary>
    /// <param name="residual">The 16 residual values (source − prediction) in raster order.</param>
    /// <param name="coeffs">Receives the 16 transform coefficients in raster order.</param>
    public static void ForwardDct(ReadOnlySpan<int> residual, Span<short> coeffs)
    {
        Span<int> tmp = stackalloc int[16];

        for (var i = 0; i < 4; i++)
        {
            var d0 = residual[i * 4 + 0];
            var d1 = residual[i * 4 + 1];
            var d2 = residual[i * 4 + 2];
            var d3 = residual[i * 4 + 3];
            var a0 = d0 + d3;
            var a1 = d1 + d2;
            var a2 = d1 - d2;
            var a3 = d0 - d3;
            tmp[0 + i * 4] = (a0 + a1) * 8;
            tmp[1 + i * 4] = (a2 * 2217 + a3 * 5352 + 1812) >> 9;
            tmp[2 + i * 4] = (a0 - a1) * 8;
            tmp[3 + i * 4] = (a3 * 2217 - a2 * 5352 + 937) >> 9;
        }

        for (var i = 0; i < 4; i++)
        {
            var a0 = tmp[0 + i] + tmp[12 + i];
            var a1 = tmp[4 + i] + tmp[8 + i];
            var a2 = tmp[4 + i] - tmp[8 + i];
            var a3 = tmp[0 + i] - tmp[12 + i];
            coeffs[0 + i] = (short)((a0 + a1 + 7) >> 4);
            coeffs[4 + i] = (short)(((a2 * 2217 + a3 * 5352 + 12000) >> 16) + (a3 != 0 ? 1 : 0));
            coeffs[8 + i] = (short)((a0 - a1 + 7) >> 4);
            coeffs[12 + i] = (short)((a3 * 2217 - a2 * 5352 + 51000) >> 16);
        }
    }

    /// <summary>Applies the inverse Walsh-Hadamard transform to the 16 luma DC coefficients.</summary>
    /// <param name="input">The 16 WHT coefficients in raster order.</param>
    /// <param name="output">Receives the 16 DC values in raster order.</param>
    public static void InverseWht(ReadOnlySpan<short> input, Span<short> output)
    {
        Span<int> tmp = stackalloc int[16];

        for (var i = 0; i < 4; i++)
        {
            var a0 = input[0 + i] + input[12 + i];
            var a1 = input[4 + i] + input[8 + i];
            var a2 = input[4 + i] - input[8 + i];
            var a3 = input[0 + i] - input[12 + i];
            tmp[0 + i] = a0 + a1;
            tmp[8 + i] = a0 - a1;
            tmp[4 + i] = a3 + a2;
            tmp[12 + i] = a3 - a2;
        }

        for (var i = 0; i < 4; i++)
        {
            var dc = tmp[0 + i * 4] + 3;
            var a0 = dc + tmp[3 + i * 4];
            var a1 = tmp[1 + i * 4] + tmp[2 + i * 4];
            var a2 = tmp[1 + i * 4] - tmp[2 + i * 4];
            var a3 = dc - tmp[3 + i * 4];
            output[i * 4 + 0] = (short)((a0 + a1) >> 3);
            output[i * 4 + 1] = (short)((a3 + a2) >> 3);
            output[i * 4 + 2] = (short)((a0 - a1) >> 3);
            output[i * 4 + 3] = (short)((a3 - a2) >> 3);
        }
    }

    /// <summary>Applies the forward Walsh-Hadamard transform to 16 DC values.</summary>
    /// <param name="input">The 16 DC values in raster order.</param>
    /// <param name="output">Receives the 16 WHT coefficients in raster order.</param>
    public static void ForwardWht(ReadOnlySpan<short> input, Span<short> output)
    {
        Span<int> tmp = stackalloc int[16];

        for (var i = 0; i < 4; i++)
        {
            var a0 = input[0 + i * 4] + input[3 + i * 4];
            var a1 = input[1 + i * 4] + input[2 + i * 4];
            var a2 = input[1 + i * 4] - input[2 + i * 4];
            var a3 = input[0 + i * 4] - input[3 + i * 4];
            tmp[0 + i * 4] = a0 + a1;
            tmp[1 + i * 4] = a3 + a2;
            tmp[2 + i * 4] = a0 - a1;
            tmp[3 + i * 4] = a3 - a2;
        }

        for (var i = 0; i < 4; i++)
        {
            var a0 = tmp[0 + i] + tmp[12 + i];
            var a1 = tmp[4 + i] + tmp[8 + i];
            var a2 = tmp[4 + i] - tmp[8 + i];
            var a3 = tmp[0 + i] - tmp[12 + i];
            var b0 = a0 + a1;
            var b1 = a3 + a2;
            var b2 = a0 - a1;
            var b3 = a3 - a2;
            output[0 + i] = (short)((b0 + (b0 > 0 ? 1 : 0)) >> 1);
            output[4 + i] = (short)((b1 + (b1 > 0 ? 1 : 0)) >> 1);
            output[8 + i] = (short)((b2 + (b2 > 0 ? 1 : 0)) >> 1);
            output[12 + i] = (short)((b3 + (b3 > 0 ? 1 : 0)) >> 1);
        }
    }

    /// <summary>Adds a residual block to an 8-bit destination block, clipping to [0, 255].</summary>
    /// <param name="block">The destination pixels (prediction), updated in place.</param>
    /// <param name="stride">The row stride of <paramref name="block"/>.</param>
    /// <param name="residual">The 16 residual values in raster order.</param>
    public static void AddResidual(Span<byte> block, int stride, ReadOnlySpan<int> residual)
    {
        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                var value = block[y * stride + x] + residual[y * 4 + x];
                block[y * stride + x] = (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
            }
        }
    }
}
