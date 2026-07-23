namespace WebPSharp.Vp8L;

/// <summary>
/// A greedy hash-chain LZ77 matcher producing the token stream for a VP8L image: a sequence of
/// literal pixels and back-references (copy length + distance). Matches are found over the ARGB
/// pixel array using a chained hash of pixel pairs; among equal-length candidates the nearest is
/// preferred. The search is fully deterministic.
/// </summary>
internal static class Vp8LLz77
{
    /// <summary>A single output token: a literal pixel or a back-reference copy.</summary>
    internal readonly struct Token
    {
        private Token(uint pixel, int length, int distance)
        {
            Pixel = pixel;
            Length = length;
            Distance = distance;
        }

        /// <summary>The literal pixel value (valid when <see cref="IsLiteral"/>).</summary>
        public uint Pixel { get; }

        /// <summary>The copy length in pixels (0 for a literal).</summary>
        public int Length { get; }

        /// <summary>The copy back-distance in pixels (valid for a copy).</summary>
        public int Distance { get; }

        /// <summary>Whether this token is a literal pixel.</summary>
        public bool IsLiteral => Length == 0;

        /// <summary>Creates a literal token.</summary>
        public static Token Literal(uint pixel) => new(pixel, 0, 0);

        /// <summary>Creates a copy token.</summary>
        public static Token Copy(int length, int distance) => new(0, length, distance);
    }

    private const int MinMatch = 2;
    private const int MaxMatch = 4096; // the VP8L length prefix code addresses up to 4096.
    private const int MaxChain = 32;
    private const int HashBits = 14;
    private const int HashSize = 1 << HashBits;

    // Distances are emitted as plane code (distance + 120); the 40-symbol distance alphabet caps
    // the representable plane code, so distances are limited to keep the plane code in range.
    private const int MaxDistance = (1 << 20) - 200;

    /// <summary>Produces the LZ77 token stream for the given pixels.</summary>
    /// <param name="argb">The ARGB pixels to compress.</param>
    /// <returns>The literal/copy tokens covering every pixel in order.</returns>
    public static List<Token> Encode(ReadOnlySpan<uint> argb)
    {
        var n = argb.Length;
        var tokens = new List<Token>(n);

        var head = new int[HashSize];
        Array.Fill(head, -1);
        var prev = new int[n];

        var i = 0;
        while (i < n)
        {
            var bestLen = 0;
            var bestDist = 0;

            if (i + 1 < n)
            {
                var h = Hash(argb[i], argb[i + 1]);
                var candidate = head[h];
                var depth = 0;
                while (candidate >= 0 && depth < MaxChain)
                {
                    var distance = i - candidate;
                    if (distance > MaxDistance)
                        break;
                    var length = MatchLength(argb, candidate, i, n);
                    if (length > bestLen)
                    {
                        bestLen = length;
                        bestDist = distance;
                        if (length >= MaxMatch)
                            break;
                    }
                    candidate = prev[candidate];
                    depth++;
                }
            }

            if (bestLen >= MinMatch)
            {
                tokens.Add(Token.Copy(bestLen, bestDist));
                var end = i + bestLen;
                while (i < end)
                {
                    Insert(argb, head, prev, i, n);
                    i++;
                }
            }
            else
            {
                tokens.Add(Token.Literal(argb[i]));
                Insert(argb, head, prev, i, n);
                i++;
            }
        }

        return tokens;
    }

    private static void Insert(ReadOnlySpan<uint> argb, int[] head, int[] prev, int pos, int n)
    {
        if (pos + 1 >= n)
            return;
        var h = Hash(argb[pos], argb[pos + 1]);
        prev[pos] = head[h];
        head[h] = pos;
    }

    private static int MatchLength(ReadOnlySpan<uint> argb, int src, int dst, int n)
    {
        var max = Math.Min(MaxMatch, n - dst);
        var k = 0;
        while (k < max && argb[src + k] == argb[dst + k])
            k++;
        return k;
    }

    private static int Hash(uint a, uint b)
    {
        var key = ((ulong)a << 32) | b;
        key *= 0x9E3779B97F4A7C15UL;
        return (int)(key >> (64 - HashBits));
    }
}
