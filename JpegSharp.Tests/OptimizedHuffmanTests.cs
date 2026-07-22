using JpegSharp.Bitstream;
using JpegSharp.Huffman;
using Xunit;

namespace JpegSharp.Tests;

public class OptimizedHuffmanTests
{
    [Fact]
    public void MoreFrequentSymbols_GetShorterOrEqualCodes()
    {
        var freq = new int[256];
        freq['A'] = 1000;
        freq['B'] = 100;
        freq['C'] = 10;
        freq['D'] = 1;

        var table = HuffmanTable.BuildOptimized(freq);

        table.GetCode('A', out _, out var sa);
        table.GetCode('B', out _, out var sb);
        table.GetCode('C', out _, out var sc);
        table.GetCode('D', out _, out var sd);

        Assert.True(sa <= sb);
        Assert.True(sb <= sc);
        Assert.True(sc <= sd);
    }

    [Fact]
    public void OnlySymbolsWithFrequency_AppearInTable()
    {
        var freq = new int[256];
        freq[10] = 5;
        freq[20] = 3;
        freq[30] = 1;

        var table = HuffmanTable.BuildOptimized(freq);

        Assert.Equal(3, table.Symbols.Length);
        var symbols = table.Symbols.ToArray();
        Assert.Contains((byte)10, symbols);
        Assert.Contains((byte)20, symbols);
        Assert.Contains((byte)30, symbols);
    }

    [Fact]
    public void SingleSymbol_GetsOneBitCode()
    {
        var freq = new int[256];
        freq[42] = 17;

        var table = HuffmanTable.BuildOptimized(freq);
        table.GetCode(42, out _, out var size);
        Assert.Equal(1, size);
    }

    [Fact]
    public void FibonacciFrequencies_AreLengthLimitedTo16()
    {
        // Fibonacci frequencies naturally yield code lengths far exceeding 16,
        // forcing the length-limiting procedure to engage.
        var freq = new int[256];
        long a = 1, b = 1;
        for (var i = 0; i < 40; i++)
        {
            freq[i] = (int)Math.Min(a, int.MaxValue);
            (a, b) = (b, a + b);
        }

        var table = HuffmanTable.BuildOptimized(freq);

        for (var i = 0; i < 40; i++)
        {
            table.GetCode(i, out _, out var size);
            Assert.InRange(size, 1, 16);
        }
    }

    [Fact]
    public void OptimizedTable_RoundTripsSymbolStream()
    {
        var rng = new Random(2024);
        var stream = new int[500];
        var freq = new int[256];
        for (var i = 0; i < stream.Length; i++)
        {
            // Skewed distribution: small values dominate.
            var symbol = (int)(Math.Pow(rng.NextDouble(), 3) * 255);
            stream[i] = symbol;
            freq[symbol]++;
        }

        var table = HuffmanTable.BuildOptimized(freq);

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        foreach (var symbol in stream)
            table.Encode(writer, symbol);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        foreach (var expected in stream)
            Assert.Equal(expected, table.DecodeSymbol(ref reader));
    }

    [Fact]
    public void OptimizedTable_IsNoWorseThanStandardTable()
    {
        var rng = new Random(7);
        var freq = new int[256];
        var standard = StandardHuffmanTables.AcLuminance;
        var alphabet = standard.Symbols.ToArray();
        // Skewed distribution over the standard AC luminance alphabet.
        for (var i = 0; i < 5000; i++)
        {
            var symbol = alphabet[(int)(Math.Pow(rng.NextDouble(), 4) * (alphabet.Length - 1))];
            freq[symbol]++;
        }

        var optimized = HuffmanTable.BuildOptimized(freq);

        long optimizedBits = 0;
        long standardBits = 0;
        for (var symbol = 0; symbol < 256; symbol++)
        {
            if (freq[symbol] == 0)
                continue;
            optimized.GetCode(symbol, out _, out var os);
            standard.GetCode(symbol, out _, out var ss);
            optimizedBits += (long)os * freq[symbol];
            standardBits += (long)ss * freq[symbol];
        }

        Assert.True(optimizedBits <= standardBits);
    }

    [Fact]
    public void WrongFrequencyLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => HuffmanTable.BuildOptimized(new int[255]));
    }

    [Fact]
    public void GeneratedCounts_SumToSymbolCount()
    {
        var freq = new int[256];
        for (var i = 0; i < 50; i++)
            freq[i * 5] = i + 1;

        var table = HuffmanTable.BuildOptimized(freq);
        var total = 0;
        foreach (var c in table.Counts)
            total += c;
        Assert.Equal(table.Symbols.Length, total);
    }
}
