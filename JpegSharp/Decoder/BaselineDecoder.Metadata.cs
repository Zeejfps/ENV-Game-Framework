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
        _iccChunks.Add((seq, segment[14..].ToArray()));
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

    private void PreserveApp(byte marker, ReadOnlySpan<byte> segment) =>
        _appSegments.Add(new JpegApplicationSegment(marker, segment.ToArray()));

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
        {
            _iccChunks.Sort(static (a, b) => a.Seq.CompareTo(b.Seq));
            var total = 0;
            foreach (var chunk in _iccChunks)
                total += chunk.Data.Length;
            var icc = new byte[total];
            var offset = 0;
            foreach (var chunk in _iccChunks)
            {
                chunk.Data.CopyTo(icc, offset);
                offset += chunk.Data.Length;
            }

            metadata.IccProfile = icc;
        }

        foreach (var comment in _comments)
            metadata.Comments.Add(comment);

        foreach (var segment in _appSegments)
            metadata.ApplicationSegments.Add(segment);

        return metadata;
    }
}
