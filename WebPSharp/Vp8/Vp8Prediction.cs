using System.Numerics;

namespace WebPSharp.Vp8;

/// <summary>
/// VP8 intra prediction for full-size blocks: the four modes shared by 16x16 luma and 8x8 chroma
/// prediction (DC, vertical, horizontal, and TrueMotion). Each fills a square block from its top
/// row, left column, and top-left corner sample. Edge macroblocks pass availability flags so DC
/// falls back correctly.
/// </summary>
internal static class Vp8Prediction
{
    /// <summary>Fills a block with the DC (average) prediction.</summary>
    /// <param name="dst">The destination block.</param>
    /// <param name="stride">The destination row stride.</param>
    /// <param name="size">The block edge length (16 or 8).</param>
    /// <param name="top">The <paramref name="size"/> samples above the block.</param>
    /// <param name="left">The <paramref name="size"/> samples left of the block.</param>
    /// <param name="hasTop">Whether the top row is available.</param>
    /// <param name="hasLeft">Whether the left column is available.</param>
    public static void FillDc(Span<byte> dst, int stride, int size,
        ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, bool hasTop, bool hasLeft)
    {
        var log2 = BitOperations.Log2((uint)size); // 4 for 16, 3 for 8
        int dc;
        if (hasTop && hasLeft)
        {
            var sum = 0;
            for (var i = 0; i < size; i++)
                sum += top[i] + left[i];
            dc = (sum + size) >> (log2 + 1);
        }
        else if (hasTop)
        {
            var sum = 0;
            for (var i = 0; i < size; i++)
                sum += top[i];
            dc = (sum + (size >> 1)) >> log2;
        }
        else if (hasLeft)
        {
            var sum = 0;
            for (var i = 0; i < size; i++)
                sum += left[i];
            dc = (sum + (size >> 1)) >> log2;
        }
        else
        {
            dc = 128;
        }

        var value = (byte)dc;
        for (var y = 0; y < size; y++)
            dst.Slice(y * stride, size).Fill(value);
    }

    /// <summary>Fills a block by copying the top row into every row (vertical prediction).</summary>
    /// <param name="dst">The destination block.</param>
    /// <param name="stride">The destination row stride.</param>
    /// <param name="size">The block edge length.</param>
    /// <param name="top">The <paramref name="size"/> samples above the block.</param>
    public static void FillVertical(Span<byte> dst, int stride, int size, ReadOnlySpan<byte> top)
    {
        for (var y = 0; y < size; y++)
            top.Slice(0, size).CopyTo(dst.Slice(y * stride, size));
    }

    /// <summary>Fills a block by spreading each left sample across its row (horizontal prediction).</summary>
    /// <param name="dst">The destination block.</param>
    /// <param name="stride">The destination row stride.</param>
    /// <param name="size">The block edge length.</param>
    /// <param name="left">The <paramref name="size"/> samples left of the block.</param>
    public static void FillHorizontal(Span<byte> dst, int stride, int size, ReadOnlySpan<byte> left)
    {
        for (var y = 0; y < size; y++)
            dst.Slice(y * stride, size).Fill(left[y]);
    }

    /// <summary>Fills a block with the TrueMotion prediction: <c>clip(left + top − topLeft)</c>.</summary>
    /// <param name="dst">The destination block.</param>
    /// <param name="stride">The destination row stride.</param>
    /// <param name="size">The block edge length.</param>
    /// <param name="top">The <paramref name="size"/> samples above the block.</param>
    /// <param name="left">The <paramref name="size"/> samples left of the block.</param>
    /// <param name="topLeft">The corner sample above-left of the block.</param>
    public static void FillTrueMotion(Span<byte> dst, int stride, int size,
        ReadOnlySpan<byte> top, ReadOnlySpan<byte> left, byte topLeft)
    {
        for (var y = 0; y < size; y++)
        {
            var baseValue = left[y] - topLeft;
            var row = dst.Slice(y * stride, size);
            for (var x = 0; x < size; x++)
            {
                var value = baseValue + top[x];
                row[x] = (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
            }
        }
    }
}
