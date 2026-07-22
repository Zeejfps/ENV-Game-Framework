using System.Runtime.CompilerServices;

namespace WebPSharp.Vp8;

/// <summary>
/// VP8 4x4 luma intra prediction (the ten B_PRED modes). Each predicts a 4x4 block from the four
/// samples directly above (A-D), the four above-and-to-the-right (E-H), the four to the left (I-L),
/// and the top-left corner (X), exactly as specified in RFC 6386.
/// </summary>
internal static class Vp8Prediction4
{
    /// <summary>The 4x4 intra prediction modes in VP8 bitstream order.</summary>
    public const int Dc = 0, TrueMotion = 1, Vertical = 2, Horizontal = 3, DownLeft = 4,
        DownRight = 5, VerticalRight = 6, VerticalLeft = 7, HorizontalDown = 8, HorizontalUp = 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Avg2(int a, int b) => (a + b + 1) >> 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Avg3(int a, int b, int c) => (a + 2 * b + c + 2) >> 2;

    /// <summary>Predicts a 4x4 block with the given mode.</summary>
    /// <param name="mode">The prediction mode (0..9).</param>
    /// <param name="dst">The destination block.</param>
    /// <param name="stride">The destination row stride.</param>
    /// <param name="top">The 8 samples above the block: A-D directly above, E-H above-right.</param>
    /// <param name="left">The 4 samples to the left of the block (I-L).</param>
    /// <param name="corner">The top-left corner sample (X).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is outside 0..9.</exception>
    public static void Predict(int mode, Span<byte> dst, int stride, ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte corner)
    {
        int A = top[0], B = top[1], C = top[2], D = top[3];
        int E = top[4], F = top[5], G = top[6], H = top[7];
        int I = left[0], J = left[1], K = left[2], L = left[3];
        int X = corner;

        // Compute into a stride-4 scratch block, then blit to the (possibly strided) destination.
        var block = new byte[16];
        void Set(int x, int y, int value) => block[y * 4 + x] = (byte)value;

        switch (mode)
        {
            case Dc:
            {
                var dc = (A + B + C + D + I + J + K + L + 4) >> 3;
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                        Set(x, y, dc);
                break;
            }
            case TrueMotion:
            {
                Span<int> topRow = [A, B, C, D];
                Span<int> leftCol = [I, J, K, L];
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                    {
                        var v = leftCol[y] + topRow[x] - X;
                        Set(x, y, v < 0 ? 0 : v > 255 ? 255 : v);
                    }
                break;
            }
            case Vertical:
            {
                var v0 = Avg3(X, A, B);
                var v1 = Avg3(A, B, C);
                var v2 = Avg3(B, C, D);
                var v3 = Avg3(C, D, E);
                for (var y = 0; y < 4; y++)
                {
                    Set(0, y, v0);
                    Set(1, y, v1);
                    Set(2, y, v2);
                    Set(3, y, v3);
                }
                break;
            }
            case Horizontal:
            {
                Span<int> rows = [Avg3(X, I, J), Avg3(I, J, K), Avg3(J, K, L), Avg3(K, L, L)];
                for (var y = 0; y < 4; y++)
                    for (var x = 0; x < 4; x++)
                        Set(x, y, rows[y]);
                break;
            }
            case DownLeft:
                Set(0, 0, Avg3(A, B, C));
                Set(1, 0, Avg3(B, C, D)); Set(0, 1, Avg3(B, C, D));
                Set(2, 0, Avg3(C, D, E)); Set(1, 1, Avg3(C, D, E)); Set(0, 2, Avg3(C, D, E));
                Set(3, 0, Avg3(D, E, F)); Set(2, 1, Avg3(D, E, F)); Set(1, 2, Avg3(D, E, F)); Set(0, 3, Avg3(D, E, F));
                Set(3, 1, Avg3(E, F, G)); Set(2, 2, Avg3(E, F, G)); Set(1, 3, Avg3(E, F, G));
                Set(3, 2, Avg3(F, G, H)); Set(2, 3, Avg3(F, G, H));
                Set(3, 3, Avg3(G, H, H));
                break;
            case DownRight:
                Set(0, 3, Avg3(J, K, L));
                Set(1, 3, Avg3(I, J, K)); Set(0, 2, Avg3(I, J, K));
                Set(2, 3, Avg3(X, I, J)); Set(1, 2, Avg3(X, I, J)); Set(0, 1, Avg3(X, I, J));
                Set(3, 3, Avg3(A, X, I)); Set(2, 2, Avg3(A, X, I)); Set(1, 1, Avg3(A, X, I)); Set(0, 0, Avg3(A, X, I));
                Set(3, 2, Avg3(B, A, X)); Set(2, 1, Avg3(B, A, X)); Set(1, 0, Avg3(B, A, X));
                Set(3, 1, Avg3(C, B, A)); Set(2, 0, Avg3(C, B, A));
                Set(3, 0, Avg3(D, C, B));
                break;
            case VerticalRight:
                Set(0, 0, Avg2(X, A)); Set(1, 2, Avg2(X, A));
                Set(1, 0, Avg2(A, B)); Set(2, 2, Avg2(A, B));
                Set(2, 0, Avg2(B, C)); Set(3, 2, Avg2(B, C));
                Set(3, 0, Avg2(C, D));
                Set(0, 3, Avg3(K, J, I));
                Set(0, 2, Avg3(J, I, X));
                Set(0, 1, Avg3(I, X, A)); Set(1, 3, Avg3(I, X, A));
                Set(1, 1, Avg3(X, A, B)); Set(2, 3, Avg3(X, A, B));
                Set(2, 1, Avg3(A, B, C)); Set(3, 3, Avg3(A, B, C));
                Set(3, 1, Avg3(B, C, D));
                break;
            case VerticalLeft:
                Set(0, 0, Avg2(A, B));
                Set(1, 0, Avg2(B, C)); Set(0, 2, Avg2(B, C));
                Set(2, 0, Avg2(C, D)); Set(1, 2, Avg2(C, D));
                Set(3, 0, Avg2(D, E)); Set(2, 2, Avg2(D, E));
                Set(0, 1, Avg3(A, B, C));
                Set(1, 1, Avg3(B, C, D)); Set(0, 3, Avg3(B, C, D));
                Set(2, 1, Avg3(C, D, E)); Set(1, 3, Avg3(C, D, E));
                Set(3, 1, Avg3(D, E, F)); Set(2, 3, Avg3(D, E, F));
                Set(3, 2, Avg3(E, F, G));
                Set(3, 3, Avg3(F, G, H));
                break;
            case HorizontalDown:
                Set(0, 0, Avg2(I, X)); Set(2, 1, Avg2(I, X));
                Set(0, 1, Avg2(J, I)); Set(2, 2, Avg2(J, I));
                Set(0, 2, Avg2(K, J)); Set(2, 3, Avg2(K, J));
                Set(0, 3, Avg2(L, K));
                Set(3, 0, Avg3(A, B, C));
                Set(2, 0, Avg3(X, A, B));
                Set(1, 0, Avg3(I, X, A)); Set(3, 1, Avg3(I, X, A));
                Set(1, 1, Avg3(J, I, X)); Set(3, 2, Avg3(J, I, X));
                Set(1, 2, Avg3(K, J, I)); Set(3, 3, Avg3(K, J, I));
                Set(1, 3, Avg3(L, K, J));
                break;
            case HorizontalUp:
                Set(0, 0, Avg2(I, J));
                Set(2, 0, Avg2(J, K)); Set(0, 1, Avg2(J, K));
                Set(2, 1, Avg2(K, L)); Set(0, 2, Avg2(K, L));
                Set(1, 0, Avg3(I, J, K));
                Set(3, 0, Avg3(J, K, L)); Set(1, 1, Avg3(J, K, L));
                Set(3, 1, Avg3(K, L, L)); Set(1, 2, Avg3(K, L, L));
                Set(3, 2, L); Set(2, 2, L); Set(0, 3, L); Set(1, 3, L); Set(2, 3, L); Set(3, 3, L);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "VP8 4x4 prediction mode must be 0..9.");
        }

        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
                dst[y * stride + x] = block[y * 4 + x];
    }
}
