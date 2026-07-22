using JpegSharp.Decoder;
using JpegSharp.Encoder;

namespace JpegSharp.Api;

/// <summary>
/// The primary entry point for encoding and decoding JPEG images. All methods are stateless
/// and thread-safe.
/// </summary>
public static class Jpeg
{
    /// <summary>Encodes an image to a JPEG byte array.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <returns>The encoded JPEG bytes.</returns>
    public static byte[] Encode(JpegImage image, JpegEncoderOptions? options = null)
    {
        using var stream = new MemoryStream();
        Encode(image, stream, options);
        return stream.ToArray();
    }

    /// <summary>Encodes an image to a stream as JPEG.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    public static void Encode(JpegImage image, Stream stream, JpegEncoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(stream);
        var encoder = new BaselineEncoder(image, options ?? new JpegEncoderOptions());
        encoder.Encode(stream);
    }

    /// <summary>Decodes a JPEG image from a byte array.</summary>
    /// <param name="data">The JPEG bytes.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded image.</returns>
    public static JpegImage Decode(byte[] data, JpegDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new BaselineDecoder(data, options).Decode();
    }

    /// <summary>
    /// Reads structural information (dimensions, components, color space, precision,
    /// progressive flag) from a JPEG stream by parsing only its headers.
    /// </summary>
    /// <param name="data">The JPEG bytes.</param>
    /// <returns>The parsed <see cref="JpegInfo"/>.</returns>
    public static JpegInfo Identify(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return new BaselineDecoder(data).ReadInfo();
    }

    /// <summary>Reads structural information from a JPEG stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>The parsed <see cref="JpegInfo"/>.</returns>
    public static JpegInfo Identify(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Identify(ms.ToArray());
    }

    /// <summary>Decodes a JPEG image from a stream.</summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <returns>The decoded image.</returns>
    public static JpegImage Decode(Stream stream, JpegDecoderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Decode(ReadAllBytes(stream), options);
    }

    /// <summary>
    /// Asynchronously reads a JPEG from a stream and decodes it. The stream is read
    /// asynchronously; the decode itself is CPU-bound and runs synchronously afterwards.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the stream read.</param>
    /// <returns>The decoded image.</returns>
    public static async Task<JpegImage> DecodeAsync(Stream stream, JpegDecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var data = await ReadAllBytesAsync(stream, cancellationToken).ConfigureAwait(false);
        return Decode(data, options);
    }

    /// <summary>
    /// Asynchronously encodes an image and writes it to a stream. Encoding is CPU-bound and
    /// runs synchronously; the resulting bytes are written to the stream asynchronously.
    /// </summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the stream write.</param>
    /// <returns>A task that completes when the bytes have been written.</returns>
    public static async Task EncodeAsync(JpegImage image, Stream stream, JpegEncoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(stream);

        // Encode synchronously into a buffer (CPU-bound), then write it out asynchronously
        // without an intermediate ToArray copy.
        using var buffer = new MemoryStream();
        Encode(image, buffer, options);
        buffer.Position = 0;
        await buffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    // Reads a stream fully into a byte array. Seekable streams (files, MemoryStreams) are read
    // into an exactly-sized buffer in a single copy; other streams fall back to buffering.
    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining is > 0 and <= int.MaxValue)
            {
                var buffer = new byte[(int)remaining];
                stream.ReadExactly(buffer);
                return buffer;
            }
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining is > 0 and <= int.MaxValue)
            {
                var buffer = new byte[(int)remaining];
                await stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
                return buffer;
            }
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    /// <summary>Loads and decodes a JPEG image from a file.</summary>
    /// <param name="path">The file path.</param>
    /// <returns>The decoded image.</returns>
    public static JpegImage Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Decode(stream);
    }

    /// <summary>Encodes an image and saves it to a file as JPEG.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    public static void Save(JpegImage image, string path, JpegEncoderOptions? options = null)
    {
        using var stream = File.Create(path);
        Encode(image, stream, options);
    }

    /// <summary>Asynchronously loads and decodes a JPEG image from a file.</summary>
    /// <param name="path">The file path.</param>
    /// <param name="options">Decoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the file read.</param>
    /// <returns>The decoded image.</returns>
    public static async Task<JpegImage> LoadAsync(string path, JpegDecoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        return await DecodeAsync(stream, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Asynchronously encodes an image and saves it to a file as JPEG.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">Encoding options, or null for defaults.</param>
    /// <param name="cancellationToken">A token to cancel the file write.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    public static async Task SaveAsync(JpegImage image, string path, JpegEncoderOptions? options = null, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(path);
        await EncodeAsync(image, stream, options, cancellationToken).ConfigureAwait(false);
    }
}
