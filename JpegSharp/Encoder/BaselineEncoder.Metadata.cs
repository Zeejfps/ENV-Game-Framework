using JpegSharp.Api;
using JpegSharp.Markers;

namespace JpegSharp.Encoder;

// Writing of metadata-bearing marker segments: JFIF (APP0), Exif (APP1), ICC (APP2, chunked),
// Adobe (APP14), comments (COM), and preserved application segments.
internal sealed partial class BaselineEncoder
{
    // Fixed-order metadata block: the exact ordering used before ordering-preservation existed, and
    // the fallback for user-constructed metadata (no manifest).
    private void WriteMetadataSegmentsFixedOrder(MarkerWriter writer)
    {
        if (_writeAdobe)
            WriteAdobe(writer);
        else
            WriteJfif(writer);
        WriteExif(writer);
        WriteIcc(writer);
        WriteComments(writer);
        WriteApplicationSegments(writer);
    }

    // Replays the source's header segment order from the decode-time manifest, then appends any typed
    // metadata the caller set after decode that the manifest did not already cover. The encoder still
    // emits exactly one mandatory APP0/APP14 (JFIF or Adobe) chosen by its own color rules rather
    // than by the source: if the manifest carries the marker the encoder writes, it is emitted at
    // that recorded position; otherwise it is emitted up front (its canonical position). A manifest
    // reference to the marker the encoder is not writing (e.g. a source JFIF while encoding Adobe)
    // is skipped so encoder correctness wins over blind replay. The tail emits set-but-unreferenced
    // Exif/ICC/comments/application segments in the canonical fixed order so nothing the caller added
    // is dropped; each segment is emitted exactly once.
    private void WriteMetadataSegmentsInOrder(MarkerWriter writer, IList<HeaderSegmentRef> order)
    {
        var mandatoryKind = _writeAdobe ? HeaderSegmentKind.Adobe : HeaderSegmentKind.Jfif;
        var manifestHasMandatory = false;
        foreach (var reference in order)
        {
            if (reference.Kind == mandatoryKind)
            {
                manifestHasMandatory = true;
                break;
            }
        }

        if (!manifestHasMandatory)
        {
            if (_writeAdobe)
                WriteAdobe(writer);
            else
                WriteJfif(writer);
        }

        var exifEmitted = false;
        var iccEmitted = false;
        var emittedComments = new HashSet<int>();
        var emittedApps = new HashSet<int>();

        foreach (var reference in order)
        {
            switch (reference.Kind)
            {
                case HeaderSegmentKind.Jfif:
                    if (!_writeAdobe)
                        WriteJfif(writer);
                    break;
                case HeaderSegmentKind.Adobe:
                    if (_writeAdobe)
                        WriteAdobe(writer);
                    break;
                case HeaderSegmentKind.Exif:
                    WriteExif(writer);
                    exifEmitted = true;
                    break;
                case HeaderSegmentKind.Icc:
                    WriteIcc(writer);
                    iccEmitted = true;
                    break;
                case HeaderSegmentKind.Comment:
                    WriteComment(writer, reference.Index);
                    emittedComments.Add(reference.Index);
                    break;
                case HeaderSegmentKind.App:
                    WriteApp(writer, reference.Index);
                    emittedApps.Add(reference.Index);
                    break;
            }
        }

        // Tail: metadata the caller set after decode that the manifest did not reference, emitted in
        // the same canonical order as the fixed-order path so it is preserved rather than dropped.
        if (!exifEmitted)
            WriteExif(writer);
        if (!iccEmitted)
            WriteIcc(writer);

        if (_metadata is not null)
        {
            var commentCount = _metadata.CommentBytes.Count > 0
                ? _metadata.CommentBytes.Count
                : _metadata.Comments.Count;
            for (var i = 0; i < commentCount; i++)
            {
                if (!emittedComments.Contains(i))
                    WriteComment(writer, i);
            }

            for (var i = 0; i < _metadata.ApplicationSegments.Count; i++)
            {
                if (!emittedApps.Contains(i))
                    WriteApp(writer, i);
            }
        }
    }

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

    // Single-comment writer for manifest replay. Mirrors WriteComments' CommentBytes-over-Comments
    // precedence: the decoder populates both lists in parallel, so the index selects the same item.
    private void WriteComment(MarkerWriter writer, int index)
    {
        if (_metadata is null)
            return;

        if (_metadata.CommentBytes.Count > 0)
        {
            if ((uint)index < (uint)_metadata.CommentBytes.Count)
                writer.WriteSegment(JpegMarkers.Comment, _metadata.CommentBytes[index]);
            return;
        }

        if ((uint)index < (uint)_metadata.Comments.Count)
            writer.WriteSegment(JpegMarkers.Comment, System.Text.Encoding.UTF8.GetBytes(_metadata.Comments[index]));
    }

    private void WriteApplicationSegments(MarkerWriter writer)
    {
        if (_metadata is null)
            return;

        foreach (var segment in _metadata.ApplicationSegments)
            writer.WriteSegment(segment.MarkerCode, segment.Data);
    }

    // Single-application-segment writer for manifest replay.
    private void WriteApp(MarkerWriter writer, int index)
    {
        if (_metadata is null || (uint)index >= (uint)_metadata.ApplicationSegments.Count)
            return;

        var segment = _metadata.ApplicationSegments[index];
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
