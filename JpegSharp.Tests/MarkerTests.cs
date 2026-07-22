using JpegSharp.Api.Exceptions;
using JpegSharp.Markers;
using Xunit;

namespace JpegSharp.Tests;

public class MarkerTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        Assert.Equal(0xD8, JpegMarkers.StartOfImage);
        Assert.Equal(0xD9, JpegMarkers.EndOfImage);
        Assert.Equal(0xC0, JpegMarkers.StartOfFrameBaseline);
        Assert.Equal(0xC2, JpegMarkers.StartOfFrameProgressive);
        Assert.Equal(0xC4, JpegMarkers.DefineHuffmanTables);
        Assert.Equal(0xDB, JpegMarkers.DefineQuantizationTables);
        Assert.Equal(0xDA, JpegMarkers.StartOfScan);
        Assert.Equal(0xDD, JpegMarkers.DefineRestartInterval);
        Assert.Equal(0xFE, JpegMarkers.Comment);
        Assert.Equal(0xE0, JpegMarkers.App0);
        Assert.Equal(0xEE, JpegMarkers.App14);
    }

    [Theory]
    [InlineData(0xD0, true)]
    [InlineData(0xD7, true)]
    [InlineData(0xD8, false)]
    [InlineData(0xC0, false)]
    public void IsRestartMarker_Classifies(byte code, bool expected)
        => Assert.Equal(expected, JpegMarkers.IsRestartMarker(code));

    [Theory]
    [InlineData(0xE0, true)]
    [InlineData(0xEF, true)]
    [InlineData(0xDA, false)]
    public void IsAppMarker_Classifies(byte code, bool expected)
        => Assert.Equal(expected, JpegMarkers.IsAppMarker(code));

    [Theory]
    [InlineData(0xC0, true)]
    [InlineData(0xC1, true)]
    [InlineData(0xC2, true)]
    [InlineData(0xC4, false)] // DHT is not a frame marker
    [InlineData(0xC8, false)] // JPG reserved
    public void IsStartOfFrame_Classifies(byte code, bool expected)
        => Assert.Equal(expected, JpegMarkers.IsStartOfFrame(code));

    [Theory]
    [InlineData(0xD8, false)] // SOI
    [InlineData(0xD9, false)] // EOI
    [InlineData(0xD0, false)] // RST0
    [InlineData(0x01, false)] // TEM
    [InlineData(0xC0, true)]  // SOF0
    [InlineData(0xDA, true)]  // SOS
    public void HasLengthField_Classifies(byte code, bool expected)
        => Assert.Equal(expected, JpegMarkers.HasLengthField(code));

    [Fact]
    public void Reader_ReadsMarkersAndSegmentPayload()
    {
        byte[] data = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x11, 0x22];
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);

        Assert.Equal(0xD8, reader.ReadMarker());
        Assert.Equal(0xE0, reader.ReadMarker());
        var payload = reader.ReadSegment();
        Assert.Equal([0x11, 0x22], payload);
    }

    [Fact]
    public void Reader_SkipsFillBytesBeforeMarker()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xD8];
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);
        Assert.Equal(0xD8, reader.ReadMarker());
    }

    [Fact]
    public void Reader_NonFfWhereMarkerExpected_Throws()
    {
        byte[] data = [0x12, 0x34];
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);
        Assert.Throws<JpegFormatException>(() => reader.ReadMarker());
    }

    [Fact]
    public void Reader_SegmentLengthTooSmall_Throws()
    {
        byte[] data = [0xFF, 0xE0, 0x00, 0x01];
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);
        reader.ReadMarker();
        Assert.Throws<JpegFormatException>(() => reader.ReadSegment());
    }

    [Fact]
    public void Reader_TruncatedPayload_Throws()
    {
        byte[] data = [0xFF, 0xE0, 0x00, 0x06, 0x11, 0x22]; // declares 4 payload bytes, only 2 present
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);
        reader.ReadMarker();
        Assert.Throws<JpegFormatException>(() => reader.ReadSegment());
    }

    [Fact]
    public void Reader_ReadUInt16_IsBigEndian()
    {
        byte[] data = [0x12, 0x34];
        using var ms = new MemoryStream(data);
        var reader = new MarkerReader(ms);
        Assert.Equal(0x1234, reader.ReadUInt16());
    }

    [Fact]
    public void Writer_WritesStandaloneMarker()
    {
        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);
        Assert.Equal([0xFF, 0xD8], ms.ToArray());
    }

    [Fact]
    public void Writer_WritesSegmentWithLengthPrefix()
    {
        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteSegment(JpegMarkers.App0, [0xAA, 0xBB, 0xCC]);
        Assert.Equal([0xFF, 0xE0, 0x00, 0x05, 0xAA, 0xBB, 0xCC], ms.ToArray());
    }

    [Fact]
    public void ReaderWriter_RoundTrip()
    {
        using var ms = new MemoryStream();
        var writer = new MarkerWriter(ms);
        writer.WriteMarker(JpegMarkers.StartOfImage);
        writer.WriteSegment(JpegMarkers.App0, [1, 2, 3, 4]);
        writer.WriteSegment(JpegMarkers.Comment, [0x48, 0x69]);
        writer.WriteMarker(JpegMarkers.EndOfImage);

        ms.Position = 0;
        var reader = new MarkerReader(ms);
        Assert.Equal(JpegMarkers.StartOfImage, reader.ReadMarker());
        Assert.Equal(JpegMarkers.App0, reader.ReadMarker());
        Assert.Equal([1, 2, 3, 4], reader.ReadSegment());
        Assert.Equal(JpegMarkers.Comment, reader.ReadMarker());
        Assert.Equal([0x48, 0x69], reader.ReadSegment());
        Assert.Equal(JpegMarkers.EndOfImage, reader.ReadMarker());
    }
}
