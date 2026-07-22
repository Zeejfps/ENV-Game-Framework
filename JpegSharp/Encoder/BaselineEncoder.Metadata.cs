using JpegSharp.Api;
using JpegSharp.Markers;

namespace JpegSharp.Encoder;

// Writing of metadata-bearing marker segments: JFIF (APP0), Exif (APP1), ICC (APP2, chunked),
// Adobe (APP14), comments (COM), and preserved application segments.
internal sealed partial class BaselineEncoder
{
    private void WriteJfif(MarkerWriter writer)
    {
        var density = _metadata?.Density ?? new JfifDensity(JpegDensityUnit.None, 1, 1);
        var x = Math.Clamp(density.X, 1, MaxDimension);
        var y = Math.Clamp(density.Y, 1, MaxDimension);
        Span<byte> payload =
        [
            (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00,
            0x01, 0x01,              // version 1.1
            (byte)density.Unit,
            (byte)(x >> 8), (byte)(x & 0xFF),
            (byte)(y >> 8), (byte)(y & 0xFF),
            0x00, 0x00,              // no thumbnail
        ];
        writer.WriteSegment(JpegMarkers.App0, payload);
    }

    private void WriteExif(MarkerWriter writer)
    {
        var exif = _metadata?.Exif;
        if (exif is null || exif.Length == 0)
            return;

        var payload = new byte[6 + exif.Length];
        payload[0] = (byte)'E';
        payload[1] = (byte)'x';
        payload[2] = (byte)'i';
        payload[3] = (byte)'f';
        payload[4] = 0;
        payload[5] = 0;
        exif.CopyTo(payload, 6);
        writer.WriteSegment(JpegMarkers.App1, payload);
    }

    private void WriteIcc(MarkerWriter writer)
    {
        var icc = _metadata?.IccProfile;
        if (icc is null || icc.Length == 0)
            return;

        ReadOnlySpan<byte> identifier = "ICC_PROFILE\0"u8;
        const int maxData = 65533 - 14; // segment limit minus (identifier + seq + count)
        var chunkCount = (icc.Length + maxData - 1) / maxData;
        if (chunkCount > 255)
            throw new JpegSharp.Api.Exceptions.JpegException("ICC profile is too large to embed (exceeds 255 chunks).");

        for (var i = 0; i < chunkCount; i++)
        {
            var offset = i * maxData;
            var length = Math.Min(maxData, icc.Length - offset);
            var payload = new byte[14 + length];
            identifier.CopyTo(payload);
            payload[12] = (byte)(i + 1);
            payload[13] = (byte)chunkCount;
            Array.Copy(icc, offset, payload, 14, length);
            writer.WriteSegment(JpegMarkers.App2, payload);
        }
    }

    private void WriteComments(MarkerWriter writer)
    {
        if (_metadata is null)
            return;

        if (_metadata.CommentBytes.Count > 0)
        {
            foreach (var comment in _metadata.CommentBytes)
                writer.WriteSegment(JpegMarkers.Comment, comment);
            return;
        }

        foreach (var comment in _metadata.Comments)
            writer.WriteSegment(JpegMarkers.Comment, System.Text.Encoding.UTF8.GetBytes(comment));
    }

    private void WriteApplicationSegments(MarkerWriter writer)
    {
        if (_metadata is null)
            return;

        foreach (var segment in _metadata.ApplicationSegments)
            writer.WriteSegment(segment.MarkerCode, segment.Data);
    }

    private void WriteAdobe(MarkerWriter writer)
    {
        // "Adobe" + version 100 + flags0 + flags1 + transform (0 = no color transform, CMYK).
        Span<byte> payload =
        [
            (byte)'A', (byte)'d', (byte)'o', (byte)'b', (byte)'e',
            0x00, 0x64, // version 100
            0x00, 0x00, // flags0
            0x00, 0x00, // flags1
            (byte)_adobeTransform,
        ];
        writer.WriteSegment(JpegMarkers.App14, payload);
    }
}
