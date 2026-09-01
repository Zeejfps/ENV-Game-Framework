namespace WebPSharp.Vp8L;

/// <summary>
/// Builds length-limited canonical prefix-code lengths from symbol frequencies. Uses the classic
/// frequency-merge (as in JPEG optimal Huffman generation) to obtain natural code lengths, then
/// applies the standard bit-length redistribution to cap the longest code at a given limit while
/// keeping the code complete. Shorter codes are assigned to more frequent symbols, so the result
/// is optimal for the resulting length multiset. Tie-breaking is deterministic.
/// </summary>
internal static class HuffmanLengthBuilder
{
    /// <summary>Builds per-symbol code lengths for the given frequencies.</summary>
    /// <param name="frequencies">Symbol frequencies; zero means the symbol is unused.</param>
    /// <param name="maxLength">The maximum allowed code length (VP8L uses 15).</param>
    /// <returns>Per-symbol code lengths; unused symbols hold zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">No symbol has a positive frequency.</exception>
    public static int[] Build(ReadOnlySpan<int> frequencies, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
        var n = frequencies.Length;
        var lengths = new int[n];

        var usedCount = 0;
        var lastUsed = -1;
        for (var i = 0; i < n; i++)
        {
            if (frequencies[i] > 0)
            {
                usedCount++;
                lastUsed = i;
            }
        }

        if (usedCount == 0)
            throw new InvalidOperationException("Cannot build a prefix code with no used symbols.");
        if (usedCount == 1)
        {
            lengths[lastUsed] = 1;
            return lengths;
        }

        // Frequency-merge to obtain a code size (depth) per symbol.
        var freq = new long[n];
        for (var i = 0; i < n; i++)
            freq[i] = frequencies[i];
        var codeSize = new int[n];
        var others = new int[n];
        Array.Fill(others, -1);

        while (true)
        {
            var c1 = FindLeast(freq, -1);
            if (c1 < 0)
                break;
            var c2 = FindLeast(freq, c1);
            if (c2 < 0)
                break;

            freq[c1] += freq[c2];
            freq[c2] = 0;

            codeSize[c1]++;
            while (others[c1] >= 0)
            {
                c1 = others[c1];
                codeSize[c1]++;
            }
            others[c1] = c2;

            codeSize[c2]++;
            while (others[c2] >= 0)
            {
                c2 = others[c2];
                codeSize[c2]++;
            }
        }

        // Histogram of code sizes over used symbols.
        var maxCodeSize = 0;
        for (var i = 0; i < n; i++)
            if (frequencies[i] > 0 && codeSize[i] > maxCodeSize)
                maxCodeSize = codeSize[i];

        var bits = new int[Math.Max(maxCodeSize, maxLength) + 1];
        for (var i = 0; i < n; i++)
            if (frequencies[i] > 0)
                bits[codeSize[i]]++;

        LimitCodeLengths(bits, maxCodeSize, maxLength);

        // Assign the shortest codes to the most frequent symbols.
        var order = new int[usedCount];
        var sortFreq = new int[n];
        var k = 0;
        for (var i = 0; i < n; i++)
        {
            sortFreq[i] = frequencies[i];
            if (frequencies[i] > 0)
                order[k++] = i;
        }
        Array.Sort(order, (a, b) =>
        {
            var cmp = sortFreq[b].CompareTo(sortFreq[a]);
            return cmp != 0 ? cmp : a.CompareTo(b);
        });

        var idx = 0;
        for (var len = 1; len <= maxLength; len++)
        {
            for (var c = 0; c < bits[len]; c++)
                lengths[order[idx++]] = len;
        }

        return lengths;
    }

    private static int FindLeast(long[] freq, int exclude)
    {
        var best = -1;
        var bestValue = long.MaxValue;
        for (var i = 0; i < freq.Length; i++)
        {
            // '<=' with ascending scan takes the larger index on ties, matching JPEG's rule.
            if (i != exclude && freq[i] > 0 && freq[i] <= bestValue)
            {
                bestValue = freq[i];
                best = i;
            }
        }
        return best;
    }

    private static void LimitCodeLengths(int[] bits, int maxCodeSize, int maxLength)
    {
        for (var i = maxCodeSize; i > maxLength; i--)
        {
            while (bits[i] > 0)
            {
                var j = i - 2;
                while (bits[j] == 0)
                    j--;
                bits[i] -= 2;
                bits[i - 1]++;
                bits[j + 1] += 2;
                bits[j]--;
            }
        }
    }
}
