using System.Buffers.Binary;
using WebPSharp.Api;
using WebPSharp.Api.Exceptions;

namespace WebPSharp.Container;

/// <summary>
/// Parses just enough of a WebP container to describe it: the format flavor, canvas dimensions,
/// and the alpha and animation flags. No pixel data is decoded.
/// </summary>
internal static class WebPHeaderReader
{
    /// <summary>The maximum width or height addressable by a VP8/VP8L 14-bit dimension field.</summary>
    internal const int MaxSimpleDimension = 1 << 14; // 16384

    /// <summary>Reads structural information from a complete WebP byte stream.</summary>
    /// <param name="source">The WebP bytes.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="WebPFormatException">The container or leading bitstream header is malformed.</exception>
    public static WebPInfo ReadInfo(ReadOnlyMemory<byte> source)
    {
        var reader = RiffReader.Create(source);
        if (!reader.MoveNext())
            throw new WebPFormatException("WebP container holds no chunks.");

        var first = reader.Current;
        var id = first.Id;

        if (id == WebPChunkIds.Vp8)
        {
            var (w, h) = ReadVp8Dimensions(first.Payload.Span);
            return new WebPInfo(w, h, WebPFormat.Lossy, hasAlpha: false, hasAnimation: false);
        }

        if (id == WebPChunkIds.Vp8L)
        {
            var (w, h, alpha) = ReadVp8LDimensions(first.Payload.Span);
            return new WebPInfo(w, h, WebPFormat.Lossless, alpha, hasAnimation: false);
        }

        if (id == WebPChunkIds.Vp8X)
        {
            var span = first.Payload.Span;
            if (span.Length < 10)
                throw new WebPFormatException($"VP8X header is truncated: {span.Length} bytes, need 10.");

            var flags = span[0];
            var hasIcc = (flags & 0x20) != 0;
            var hasAlpha = (flags & 0x10) != 0;
            var hasExif = (flags & 0x08) != 0;
            var hasXmp = (flags & 0x04) != 0;
            var hasAnimation = (flags & 0x02) != 0;
            var width = ReadUInt24LittleEndian(span, 4) + 1;
            var height = ReadUInt24LittleEndian(span, 7) + 1;

            var frameCount = 0;
            var loopCount = 0;
            if (hasAnimation)
            {
                while (reader.MoveNext())
                {
                    var chunk = reader.Current;
                    if (chunk.Id == WebPChunkIds.Anmf)
                        frameCount++;
                    else if (chunk.Id == WebPChunkIds.Anim && chunk.Payload.Length >= 6)
                        loopCount = BinaryPrimitives.ReadUInt16LittleEndian(chunk.Payload.Span.Slice(4, 2));
                }
            }

            return new WebPInfo(width, height, WebPFormat.Extended, hasAlpha, hasAnimation,
                hasIcc, hasExif, hasXmp, frameCount, loopCount);
        }

        throw new WebPFormatException($"Unexpected leading chunk '{id}'; expected 'VP8 ', 'VP8L', or 'VP8X'.");
    }

    /// <summary>Reads the width and height from a VP8 (lossy) keyframe header.</summary>
    /// <param name="payload">The <c>VP8&#160;</c> chunk payload.</param>
    /// <returns>The decoded dimensions.</returns>
    /// <exception cref="WebPFormatException">The frame tag or start code is invalid.</exception>
    public static (int Width, int Height) ReadVp8Dimensions(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 10)
            throw new WebPFormatException($"VP8 header is truncated: {payload.Length} bytes, need at least 10.");

        var frameTag = payload[0] | (payload[1] << 8) | (payload[2] << 16);
        var keyFrame = (frameTag & 1) == 0;
        if (!keyFrame)
            throw new WebPFormatException("VP8 bitstream does not begin with a key frame.");

        if (payload[3] != 0x9D || payload[4] != 0x01 || payload[5] != 0x2A)
            throw new WebPFormatException("VP8 key frame is missing its 0x9D 0x01 0x2A start code.");

        var width = (payload[6] | (payload[7] << 8)) & 0x3FFF;
        var height = (payload[8] | (payload[9] << 8)) & 0x3FFF;
        if (width == 0 || height == 0)
            throw new WebPFormatException($"VP8 header declares a zero dimension ({width}x{height}).");
        return (width, height);
    }

    /// <summary>Reads the width, height, and alpha flag from a VP8L (lossless) header.</summary>
    /// <param name="payload">The <c>VP8L</c> chunk payload.</param>
    /// <returns>The decoded dimensions and whether alpha is used.</returns>
    /// <exception cref="WebPFormatException">The signature byte or version is invalid.</exception>
    public static (int Width, int Height, bool HasAlpha) ReadVp8LDimensions(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
            throw new WebPFormatException($"VP8L header is truncated: {payload.Length} bytes, need at least 5.");

        if (payload[0] != 0x2F)
            throw new WebPFormatException($"VP8L header is missing its 0x2F signature (found 0x{payload[0]:X2}).");

        // Four bytes after the signature carry, LSB-first: width-1 (14), height-1 (14), alpha (1), version (3).
        var bits = (uint)(payload[1] | (payload[2] << 8) | (payload[3] << 16) | (payload[4] << 24));
        var width = (int)(bits & 0x3FFF) + 1;
        var height = (int)((bits >> 14) & 0x3FFF) + 1;
        var alpha = ((bits >> 28) & 1) != 0;
        var version = (bits >> 29) & 7;
        if (version != 0)
            throw new WebPFormatException($"Unsupported VP8L version {version}; only version 0 is defined.");
        return (width, height, alpha);
    }

    /// <summary>Reads canvas info and feature flags from a VP8X extended header.</summary>
    /// <param name="payload">The <c>VP8X</c> chunk payload.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="WebPFormatException">The header length is wrong.</exception>
    public static WebPInfo ReadExtendedInfo(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 10)
            throw new WebPFormatException($"VP8X header is truncated: {payload.Length} bytes, need 10.");

        var flags = payload[0];
        var hasAlpha = (flags & 0x10) != 0;
        var hasAnimation = (flags & 0x02) != 0;

        var width = (payload[4] | (payload[5] << 8) | (payload[6] << 16)) + 1;
        var height = (payload[7] | (payload[8] << 8) | (payload[9] << 16)) + 1;
        return new WebPInfo(width, height, WebPFormat.Extended, hasAlpha, hasAnimation);
    }

    /// <summary>Reads the little-endian 24-bit value at <paramref name="offset"/>.</summary>
    /// <param name="span">The source span.</param>
    /// <param name="offset">The starting byte offset.</param>
    /// <returns>The 24-bit value.</returns>
    internal static int ReadUInt24LittleEndian(ReadOnlySpan<byte> span, int offset)
        => span[offset] | (span[offset + 1] << 8) | (span[offset + 2] << 16);

    /// <summary>Writes a little-endian 24-bit value at <paramref name="offset"/>.</summary>
    /// <param name="span">The destination span.</param>
    /// <param name="offset">The starting byte offset.</param>
    /// <param name="value">The 24-bit value to write (0..16777215).</param>
    internal static void WriteUInt24LittleEndian(Span<byte> span, int offset, int value)
    {
        span[offset] = (byte)value;
        span[offset + 1] = (byte)(value >> 8);
        span[offset + 2] = (byte)(value >> 16);
    }

    /// <summary>Reads a little-endian 32-bit value (helper mirroring <see cref="BinaryPrimitives"/>).</summary>
    /// <param name="span">The source span.</param>
    /// <returns>The 32-bit value.</returns>
    internal static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> span)
        => BinaryPrimitives.ReadUInt32LittleEndian(span);
}
