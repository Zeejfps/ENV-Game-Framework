using JpegSharp.Api;

namespace JpegSharp.Decoder;

// Parsing of metadata-bearing marker segments (JFIF, Exif, ICC, Adobe) and assembly of the
// resulting JpegMetadata.
internal sealed partial class BaselineDecoder
{
    private bool ParseJfif(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 14 ||
            segment[0] != 'J' || segment[1] != 'F' || segment[2] != 'I' || segment[3] != 'F' || segment[4] != 0)
        {
            return false;
        }

        var unit = (JpegDensityUnit)segment[7];
        var x = (segment[8] << 8) | segment[9];
        var y = (segment[10] << 8) | segment[11];
        _density = new JfifDensity(unit, x, y);
        return true;
    }

    private bool ParseExif(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 6 ||
            segment[0] != 'E' || segment[1] != 'x' || segment[2] != 'i' || segment[3] != 'f' || segment[4] != 0 || segment[5] != 0)
        {
            return false;
        }

        _exif = segment[6..].ToArray();
        return true;
    }

    private bool ParseIccChunk(ReadOnlySpan<byte> segment)
    {
        ReadOnlySpan<byte> identifier = "ICC_PROFILE\0"u8;
        if (segment.Length < 14 || !segment[..12].SequenceEqual(identifier))
            return false;

        var seq = segment[12];
        var count = segment[13];
        _iccChunks.Add((seq, count, segment[14..].ToArray()));
        return true;
    }

    private bool ParseAdobe(ReadOnlySpan<byte> segment)
    {
        if (segment.Length < 12 ||
            segment[0] != 'A' || segment[1] != 'd' || segment[2] != 'o' || segment[3] != 'b' || segment[4] != 'e')
        {
            return false;
        }

        _adobeTransform = segment[11];
        return true;
    }

    private void PreserveApp(byte marker, ReadOnlySpan<byte> segment)
    {
        _appSegments.Add(new JpegApplicationSegment(marker, segment.ToArray()));
        _headerOrder.Add(new HeaderSegmentRef(HeaderSegmentKind.App, _appSegments.Count - 1));
    }

    // Captures a COM segment (from the header or from between scans) into the comment lists and
    // appends it to the ordering manifest so it is re-emitted on encode.
    private void CaptureComment(ReadOnlySpan<byte> segment)
    {
        var commentBytes = segment.ToArray();
        _commentBytes.Add(commentBytes);
        _comments.Add(System.Text.Encoding.UTF8.GetString(commentBytes));
        _headerOrder.Add(new HeaderSegmentRef(HeaderSegmentKind.Comment, _commentBytes.Count - 1));
    }

    // Records a single-instance typed segment (JFIF, Exif, ICC, Adobe) in the ordering manifest the
    // first time it is encountered; the encoder emits each of these once, so duplicates are ignored.
    private void RecordOnce(HeaderSegmentKind kind, ref bool recorded)
    {
        if (recorded)
            return;
        _headerOrder.Add(new HeaderSegmentRef(kind, 0));
        recorded = true;
    }

    private JpegMetadata BuildMetadata()
    {
        var metadata = new JpegMetadata
        {
            Density = _density,
            Exif = _exif,
        };

        if (_adobeTransform >= 0)
            metadata.AdobeColorTransform = _adobeTransform;

        if (_iccChunks.Count > 0)
            metadata.IccProfile = ReassembleIccProfile();

        foreach (var comment in _comments)
            metadata.Comments.Add(comment);

        foreach (var commentBytes in _commentBytes)
            metadata.CommentBytes.Add(commentBytes);

        foreach (var segment in _appSegments)
            metadata.ApplicationSegments.Add(segment);

        foreach (var reference in _headerOrder)
            metadata.HeaderSegmentOrder.Add(reference);

        return metadata;
    }

    // Reassembles the ICC profile from its APP2 chunks per ICC.1 Annex B (T.872). A well-formed
    // set carries seqNo 1..count with a consistent count and no gaps; identical duplicates are
    // tolerated. If the set is malformed in any way (inconsistent count, seq out of range,
    // conflicting duplicate, or a missing chunk) the profile is DROPPED (returns null) rather than
    // emitting a silently corrupt concatenation.
    private byte[]? ReassembleIccProfile()
    {
        var count = _iccChunks[0].Count;
        foreach (var chunk in _iccChunks)
        {
            if (chunk.Count != count)
                return null; // chunks disagree on total count
        }

        if (count < 1 || count > 255)
            return null;

        var bySeq = new byte[count + 1][];
        foreach (var chunk in _iccChunks)
        {
            if (chunk.Seq < 1 || chunk.Seq > count)
                return null; // seq=0 or seq>count is out of range

            var existing = bySeq[chunk.Seq];
            if (existing is null)
                bySeq[chunk.Seq] = chunk.Data;
            else if (!existing.AsSpan().SequenceEqual(chunk.Data))
                return null; // same seq with conflicting data
        }

        var total = 0;
        for (var seq = 1; seq <= count; seq++)
        {
            if (bySeq[seq] is null)
                return null; // gap: a chunk is missing
            total += bySeq[seq].Length;
        }

        var icc = new byte[total];
        var offset = 0;
        for (var seq = 1; seq <= count; seq++)
        {
            bySeq[seq].CopyTo(icc, offset);
            offset += bySeq[seq].Length;
        }

        return icc;
    }
}
