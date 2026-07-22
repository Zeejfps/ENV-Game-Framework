using System.Buffers.Binary;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;
using WebPSharp.Vp8L;

namespace WebPSharp.Api;

/// <summary>
/// The primary entry point for working with WebP images. All methods are stateless and
/// thread-safe.
/// </summary>
/// <remarks>
/// The codec is being implemented incrementally. Container inspection (<see cref="Identify(byte[])"/>)
/// and lossless (VP8L) encode/decode are available now; lossy (VP8), animation, and metadata
/// support are layered on in subsequent iterations. Operations that meet an unimplemented feature
/// raise a descriptive <see cref="WebPException"/> rather than mis-decoding.
/// </remarks>
public static class WebP
{
    /// <summary>
    /// Reads structural information (dimensions, format, alpha and animation flags) from a WebP
    /// stream by parsing only its RIFF container and leading bitstream header.
    /// </summary>
    /// <param name="data">The WebP bytes.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="WebPFormatException">The data is not a valid WebP container.</exception>
    public static WebPInfo Identify(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return WebPHeaderReader.ReadInfo(data);
    }

    /// <summary>Reads structural information from a WebP stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    /// <exception cref="WebPFormatException">The data is not a valid WebP container.</exception>
    public static WebPInfo Identify(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return WebPHeaderReader.ReadInfo(ms.GetBuffer().AsMemory(0, (int)ms.Length));
    }

    /// <summary>Reads structural information from a WebP file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="WebPFormatException">The data is not a valid WebP container.</exception>
    public static WebPInfo IdentifyFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return WebPHeaderReader.ReadInfo(File.ReadAllBytes(path));
    }

    /// <summary>Decodes a WebP image from a byte array.</summary>
    /// <param name="data">The WebP bytes.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="WebPFormatException">The container is malformed.</exception>
    /// <exception cref="WebPException">The file uses a feature not yet supported.</exception>
    public static WebPImage Decode(byte[] data, WebPDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Decode((ReadOnlyMemory<byte>)data, options ?? new WebPDecoderOptions());
    }

    /// <summary>Decodes a WebP image from a stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static WebPImage Decode(Stream stream, WebPDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Decode(ms.GetBuffer().AsMemory(0, (int)ms.Length), options ?? new WebPDecoderOptions());
    }

    /// <summary>Loads and decodes a WebP image from a file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static WebPImage Load(string path, WebPDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Decode(File.ReadAllBytes(path), options);
    }

    /// <summary>Encodes an image to a WebP byte array.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <returns>The encoded WebP bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="WebPException">A requested feature is not yet supported.</exception>
    public static byte[] Encode(WebPImage image, WebPEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        options ??= new WebPEncoderOptions();
        if (!options.Lossless)
            throw new WebPException("Lossy (VP8) encoding is not yet supported; set WebPEncoderOptions.Lossless = true.");

        var effort = Math.Clamp(options.Effort, 0, 9);
        var payload = Vp8LEncoder.EncodeBest(image, effort);

        using var ms = new MemoryStream(payload.Length + 64);
        var writer = new RiffWriter(ms);
        if (HasMetadata(image.Metadata))
            WriteExtended(writer, image, payload);
        else
            writer.WriteChunk(WebPChunkIds.Vp8L, payload);
        writer.Complete();
        return ms.ToArray();
    }

    private static byte[] SetOnce(byte[]? existing, RiffChunk chunk, string name)
    {
        if (existing is not null)
            throw new WebPFormatException($"Duplicate {name} chunk.");
        return chunk.Payload.ToArray();
    }

    private static bool HasMetadata(WebPMetadata? metadata) =>
        metadata is not null &&
        (metadata.IccProfile is { Length: > 0 } ||
         metadata.Exif is { Length: > 0 } ||
         metadata.Xmp is { Length: > 0 } ||
         metadata.UnknownChunks.Count > 0);

    private static void WriteExtended(RiffWriter writer, WebPImage image, byte[] imagePayload)
    {
        var metadata = image.Metadata!;

        // VP8X feature flags: ICC, alpha, EXIF, XMP (animation is written by the animation path).
        byte flags = 0;
        if (metadata.IccProfile is { Length: > 0 }) flags |= 0x20;
        if (image.HasAlpha) flags |= 0x10;
        if (metadata.Exif is { Length: > 0 }) flags |= 0x08;
        if (metadata.Xmp is { Length: > 0 }) flags |= 0x04;

        Span<byte> vp8x = stackalloc byte[10];
        vp8x[0] = flags;
        WebPHeaderReader.WriteUInt24LittleEndian(vp8x, 4, image.Width - 1);
        WebPHeaderReader.WriteUInt24LittleEndian(vp8x, 7, image.Height - 1);
        writer.WriteChunk(WebPChunkIds.Vp8X, vp8x);

        // Spec chunk order: VP8X, ICCP, image, EXIF, XMP, then any preserved unknown chunks.
        if (metadata.IccProfile is { Length: > 0 } icc)
            writer.WriteChunk(WebPChunkIds.Iccp, icc);
        writer.WriteChunk(WebPChunkIds.Vp8L, imagePayload);
        if (metadata.Exif is { Length: > 0 } exif)
            writer.WriteChunk(WebPChunkIds.Exif, exif);
        if (metadata.Xmp is { Length: > 0 } xmp)
            writer.WriteChunk(WebPChunkIds.Xmp, xmp);
        foreach (var chunk in metadata.UnknownChunks)
            writer.WriteChunk(new FourCc(chunk.Id), chunk.Data);
    }

    /// <summary>Encodes an image to a stream as WebP.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> or <paramref name="stream"/> is null.</exception>
    public static void Encode(WebPImage image, Stream stream, WebPEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = Encode(image, options);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Encodes an image and saves it to a file as WebP.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> or <paramref name="path"/> is null.</exception>
    public static void Save(WebPImage image, string path, WebPEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(path);
        File.WriteAllBytes(path, Encode(image, options));
    }

    /// <summary>Encodes an animation to an extended (animated) WebP byte array.</summary>
    /// <param name="animation">The animation to encode.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <returns>The encoded animated WebP bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The animation has no frames.</exception>
    /// <exception cref="WebPException">A requested feature is not yet supported.</exception>
    public static byte[] EncodeAnimation(WebPAnimation animation, WebPEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(animation);
        options ??= new WebPEncoderOptions();
        if (!options.Lossless)
            throw new WebPException("Lossy (VP8) animation encoding is not yet supported.");
        if (animation.Frames.Count == 0)
            throw new InvalidOperationException("Animation has no frames.");

        var metadata = animation.Metadata;
        var hasAlpha = false;
        foreach (var f in animation.Frames)
            hasAlpha |= f.Image.HasAlpha;

        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);

        byte flags = 0x02; // animation
        if (hasAlpha) flags |= 0x10;
        if (metadata?.IccProfile is { Length: > 0 }) flags |= 0x20;
        if (metadata?.Exif is { Length: > 0 }) flags |= 0x08;
        if (metadata?.Xmp is { Length: > 0 }) flags |= 0x04;

        Span<byte> vp8x = stackalloc byte[10];
        vp8x[0] = flags;
        WebPHeaderReader.WriteUInt24LittleEndian(vp8x, 4, animation.Width - 1);
        WebPHeaderReader.WriteUInt24LittleEndian(vp8x, 7, animation.Height - 1);
        writer.WriteChunk(WebPChunkIds.Vp8X, vp8x);

        if (metadata?.IccProfile is { Length: > 0 } icc)
            writer.WriteChunk(WebPChunkIds.Iccp, icc);

        Span<byte> animChunk = stackalloc byte[6];
        var bg = animation.BackgroundColor;
        animChunk[0] = (byte)bg;         // blue
        animChunk[1] = (byte)(bg >> 8);  // green
        animChunk[2] = (byte)(bg >> 16); // red
        animChunk[3] = (byte)(bg >> 24); // alpha
        BinaryPrimitives.WriteUInt16LittleEndian(animChunk.Slice(4, 2), (ushort)animation.LoopCount);
        writer.WriteChunk(WebPChunkIds.Anim, animChunk);

        foreach (var frame in animation.Frames)
            writer.WriteChunk(WebPChunkIds.Anmf, BuildFrameChunk(frame));

        if (metadata?.Exif is { Length: > 0 } exif)
            writer.WriteChunk(WebPChunkIds.Exif, exif);
        if (metadata?.Xmp is { Length: > 0 } xmp)
            writer.WriteChunk(WebPChunkIds.Xmp, xmp);

        writer.Complete();
        return ms.ToArray();
    }

    /// <summary>Decodes an animated WebP into a <see cref="WebPAnimation"/>.</summary>
    /// <param name="data">The animated WebP bytes.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded animation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is null.</exception>
    /// <exception cref="WebPFormatException">The file is not an animated WebP.</exception>
    public static WebPAnimation DecodeAnimation(byte[] data, WebPDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        options ??= new WebPDecoderOptions();

        var reader = RiffReader.Create(data);
        if (!reader.MoveNext() || reader.Current.Id != WebPChunkIds.Vp8X)
            throw new WebPFormatException("Animated WebP must begin with a VP8X chunk.");

        var info = WebPHeaderReader.ReadExtendedInfo(reader.Current.Payload.Span);
        if (!info.HasAnimation)
            throw new WebPFormatException("WebP does not contain animation frames.");

        var animation = new WebPAnimation(info.Width, info.Height);
        var metadata = options.ReadMetadata ? new WebPMetadata() : null;
        var sawAnim = false;

        while (reader.MoveNext())
        {
            var chunk = reader.Current;
            var id = chunk.Id;
            if (id == WebPChunkIds.Anim)
            {
                if (sawAnim)
                    throw new WebPFormatException("Duplicate ANIM chunk.");
                ParseAnimChunk(chunk.Payload.Span, animation);
                sawAnim = true;
            }
            else if (id == WebPChunkIds.Anmf)
            {
                if (!sawAnim)
                    throw new WebPFormatException("ANMF frame chunk appears before the ANIM chunk.");
                animation.Frames.Add(ParseFrameChunk(chunk.Payload, options));
            }
            else if (metadata is null)
            {
                // Skip metadata when disabled.
            }
            else if (id == WebPChunkIds.Iccp)
                metadata.IccProfile = SetOnce(metadata.IccProfile, chunk, "ICCP");
            else if (id == WebPChunkIds.Exif)
                metadata.Exif = SetOnce(metadata.Exif, chunk, "EXIF");
            else if (id == WebPChunkIds.Xmp)
                metadata.Xmp = SetOnce(metadata.Xmp, chunk, "XMP");
            else
                metadata.UnknownChunks.Add(new WebPUnknownChunk(id.ToString(), chunk.Payload.ToArray()));
        }

        if (!sawAnim)
            throw new WebPFormatException("Animated WebP is missing its ANIM chunk.");
        if (metadata is not null && HasMetadata(metadata))
            animation.Metadata = metadata;
        return animation;
    }

    /// <summary>Encodes an animation to a stream as an animated WebP.</summary>
    /// <param name="animation">The animation to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> or <paramref name="stream"/> is null.</exception>
    public static void EncodeAnimation(WebPAnimation animation, Stream stream, WebPEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = EncodeAnimation(animation, options);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Asynchronously encodes an animation to a stream as an animated WebP.</summary>
    /// <param name="animation">The animation to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="animation"/> or <paramref name="stream"/> is null.</exception>
    public static async Task EncodeAnimationAsync(WebPAnimation animation, Stream stream, WebPEncoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = EncodeAnimation(animation, options);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Decodes an animated WebP from a stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded animation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static WebPAnimation DecodeAnimation(Stream stream, WebPDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return DecodeAnimation(ms.ToArray(), options);
    }

    /// <summary>Asynchronously decodes an animated WebP from a stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The decoded animation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static async Task<WebPAnimation> DecodeAnimationAsync(Stream stream, WebPDecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return DecodeAnimation(data, options);
    }

    private static byte[] BuildFrameChunk(WebPFrame frame)
    {
        var payload = Vp8LEncoder.Encode(frame.Image, new Vp8LEncodeSettings { Lz77 = true });

        using var fs = new MemoryStream(payload.Length + 32);
        Span<byte> header = stackalloc byte[16];
        WebPHeaderReader.WriteUInt24LittleEndian(header, 0, frame.X / 2);
        WebPHeaderReader.WriteUInt24LittleEndian(header, 3, frame.Y / 2);
        WebPHeaderReader.WriteUInt24LittleEndian(header, 6, frame.Width - 1);
        WebPHeaderReader.WriteUInt24LittleEndian(header, 9, frame.Height - 1);
        WebPHeaderReader.WriteUInt24LittleEndian(header, 12, frame.DurationMs);
        byte flags = 0;
        if (frame.Blend == WebPBlendMethod.Source) flags |= 0x02;
        if (frame.Disposal == WebPDisposalMethod.Background) flags |= 0x01;
        header[15] = flags;
        fs.Write(header);

        RiffWriter.WriteChunkTo(fs, WebPChunkIds.Vp8L, payload);
        return fs.ToArray();
    }

    private static void ParseAnimChunk(ReadOnlySpan<byte> payload, WebPAnimation animation)
    {
        if (payload.Length < 6)
            throw new WebPFormatException("ANIM chunk is truncated.");
        animation.BackgroundColor =
            ((uint)payload[3] << 24) | ((uint)payload[2] << 16) | ((uint)payload[1] << 8) | payload[0];
        animation.LoopCount = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(4, 2));
    }

    private static WebPFrame ParseFrameChunk(ReadOnlyMemory<byte> payload, WebPDecoderOptions options)
    {
        var span = payload.Span;
        if (span.Length < 16)
            throw new WebPFormatException("ANMF chunk header is truncated.");

        var x = WebPHeaderReader.ReadUInt24LittleEndian(span, 0) * 2;
        var y = WebPHeaderReader.ReadUInt24LittleEndian(span, 3) * 2;
        var duration = WebPHeaderReader.ReadUInt24LittleEndian(span, 12);
        var flags = span[15];
        var blend = (flags & 0x02) != 0 ? WebPBlendMethod.Source : WebPBlendMethod.Over;
        var disposal = (flags & 0x01) != 0 ? WebPDisposalMethod.Background : WebPDisposalMethod.None;

        WebPImage? image = null;
        var inner = payload[16..];
        var innerSpan = inner.Span;
        var pos = 0;
        while (pos + RiffReader.HeaderSize <= innerSpan.Length)
        {
            var cid = FourCc.Read(innerSpan.Slice(pos, 4));
            var size = (int)BinaryPrimitives.ReadUInt32LittleEndian(innerSpan.Slice(pos + 4, 4));
            var start = pos + RiffReader.HeaderSize;
            if (size < 0 || start + size > innerSpan.Length)
                throw new WebPFormatException("Animation frame sub-chunk overruns the frame payload.");

            if (cid == WebPChunkIds.Vp8L)
                image = DecodeLossless(inner.Slice(start, size), options);
            else if (cid == WebPChunkIds.Vp8)
                throw new WebPException("Lossy (VP8) animation frame decoding is not yet supported.");

            pos = start + size + (size & 1);
        }

        if (image is null)
            throw new WebPFormatException("Animation frame has no supported image chunk.");

        return new WebPFrame(image, x, y, duration, blend, disposal);
    }

    /// <summary>Asynchronously decodes a WebP image from a stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static async Task<WebPImage> DecodeAsync(Stream stream, WebPDecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return Decode(data, options ?? new WebPDecoderOptions());
    }

    /// <summary>Asynchronously encodes an image to a stream as WebP.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> or <paramref name="stream"/> is null.</exception>
    public static async Task EncodeAsync(WebPImage image, Stream stream, WebPEncoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(stream);
        var bytes = Encode(image, options);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Asynchronously loads and decodes a WebP image from a file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The decoded image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static async Task<WebPImage> LoadAsync(string path, WebPDecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return Decode(bytes, options);
    }

    /// <summary>Asynchronously encodes an image and saves it to a file as WebP.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> or <paramref name="path"/> is null.</exception>
    public static async Task SaveAsync(WebPImage image, string path, WebPEncoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(path);
        var bytes = Encode(image, options);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Asynchronously reads structural information from a WebP stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The parsed <see cref="WebPInfo"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
    public static async Task<WebPInfo> IdentifyAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
        return WebPHeaderReader.ReadInfo(data);
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return ms.ToArray();
    }

    private static WebPImage Decode(ReadOnlyMemory<byte> data, WebPDecoderOptions options)
    {
        var reader = RiffReader.Create(data);
        if (!reader.MoveNext())
            throw new WebPFormatException("WebP container holds no chunks.");

        var first = reader.Current;
        if (first.Id == WebPChunkIds.Vp8L)
            return DecodeLossless(first.Payload, options);
        if (first.Id == WebPChunkIds.Vp8)
            throw new WebPException("Lossy (VP8) decoding is not yet supported.");
        if (first.Id == WebPChunkIds.Vp8X)
            return DecodeExtended(ref reader, first.Payload, options);

        throw new WebPFormatException($"Unexpected leading chunk '{first.Id}'; expected 'VP8 ', 'VP8L', or 'VP8X'.");
    }

    private static WebPImage DecodeExtended(ref RiffReader reader, ReadOnlyMemory<byte> vp8xPayload, WebPDecoderOptions options)
    {
        WebPImage? image = null;
        var metadata = options.ReadMetadata ? new WebPMetadata() : null;

        while (reader.MoveNext())
        {
            var chunk = reader.Current;
            var id = chunk.Id;

            if (id == WebPChunkIds.Vp8L || id == WebPChunkIds.Vp8)
            {
                if (image is not null)
                    throw new WebPFormatException("Extended container has more than one image chunk.");
                if (id == WebPChunkIds.Vp8)
                    throw new WebPException("Lossy (VP8) decoding is not yet supported.");
                image = DecodeLossless(chunk.Payload, options);
            }
            else if (id == WebPChunkIds.Anim || id == WebPChunkIds.Anmf)
                throw new WebPException("Animated WebP decoding is not yet supported.");
            else if (id == WebPChunkIds.Vp8X)
                throw new WebPFormatException("Duplicate VP8X chunk.");
            else if (metadata is null || id == WebPChunkIds.Alph)
            {
                // Metadata disabled, or ALPH (handled with its VP8 image, which is unsupported here).
            }
            else if (id == WebPChunkIds.Iccp)
                metadata.IccProfile = SetOnce(metadata.IccProfile, chunk, "ICCP");
            else if (id == WebPChunkIds.Exif)
                metadata.Exif = SetOnce(metadata.Exif, chunk, "EXIF");
            else if (id == WebPChunkIds.Xmp)
                metadata.Xmp = SetOnce(metadata.Xmp, chunk, "XMP");
            else
                metadata.UnknownChunks.Add(new WebPUnknownChunk(id.ToString(), chunk.Payload.ToArray()));
        }

        if (image is null)
            throw new WebPFormatException("Extended (VP8X) container has no supported image chunk.");

        var vp8x = vp8xPayload.Span;
        if (vp8x.Length >= 10)
        {
            var canvasWidth = WebPHeaderReader.ReadUInt24LittleEndian(vp8x, 4) + 1;
            var canvasHeight = WebPHeaderReader.ReadUInt24LittleEndian(vp8x, 7) + 1;
            if (image.Width != canvasWidth || image.Height != canvasHeight)
                throw new WebPFormatException(
                    $"VP8X canvas {canvasWidth}x{canvasHeight} does not match the image {image.Width}x{image.Height}.");
        }

        if (metadata is not null && HasMetadata(metadata))
            image.Metadata = metadata;
        return image;
    }

    private static WebPImage DecodeLossless(ReadOnlyMemory<byte> payload, WebPDecoderOptions options)
    {
        var (width, height, _) = WebPHeaderReader.ReadVp8LDimensions(payload.Span);
        if ((long)width * height > options.MaxPixels)
            throw new WebPFormatException($"Image dimensions {width}x{height} exceed the {options.MaxPixels}-pixel limit.");
        return Vp8LDecoder.Decode(payload.Span);
    }
}
