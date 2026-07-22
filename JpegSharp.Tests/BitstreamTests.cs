using JpegSharp.Bitstream;
using Xunit;

namespace JpegSharp.Tests;

public class BitstreamTests
{
    [Fact]
    public void ReadBits_ReadsMsbFirstAcrossByteBoundary()
    {
        byte[] data = [0b1011_0010, 0b1100_0000];
        var reader = new BitReader(data);

        Assert.Equal(0b101, reader.ReadBits(3));
        Assert.Equal(0b10010, reader.ReadBits(5));
        Assert.Equal(0b110, reader.ReadBits(3));
    }

    [Fact]
    public void ReadBits_Zero_ReturnsZeroWithoutConsuming()
    {
        byte[] data = [0xAB];
        var reader = new BitReader(data);
        Assert.Equal(0, reader.ReadBits(0));
        Assert.Equal(0xAB, reader.ReadBits(8));
    }

    [Fact]
    public void ByteStuffing_Ff00_DecodesToSingleFfByte()
    {
        byte[] data = [0xFF, 0x00, 0x2A];
        var reader = new BitReader(data);
        Assert.Equal(0xFF, reader.ReadBits(8));
        Assert.Equal(0x2A, reader.ReadBits(8));
        Assert.False(reader.MarkerReached);
    }

    [Fact]
    public void Marker_StopsBitConsumptionAndIsExposed()
    {
        byte[] data = [0x12, 0xFF, 0xD9];
        var reader = new BitReader(data);
        Assert.Equal(0x12, reader.ReadBits(8));

        // Reading past the entropy data hits the marker; padding bits read as 1s.
        Assert.Equal(0xFF, reader.ReadBits(8));
        Assert.True(reader.MarkerReached);
        Assert.Equal(0xD9, reader.Marker);
    }

    [Fact]
    public void Marker_LeavesBytePositionAtMarkerStart()
    {
        byte[] data = [0x12, 0xFF, 0xD0, 0x34];
        var reader = new BitReader(data);
        reader.ReadBits(8);
        reader.ReadBits(8); // triggers marker detection
        Assert.True(reader.MarkerReached);
        Assert.Equal(0xD0, reader.Marker);
        Assert.Equal(1, reader.BytePosition); // points at the 0xFF
    }

    [Fact]
    public void ResetForRestart_SkipsMarkerAndContinues()
    {
        byte[] data = [0x12, 0xFF, 0xD2, 0x56];
        var reader = new BitReader(data);
        reader.ReadBits(8);
        reader.ReadBits(8); // detect RST2
        Assert.True(reader.MarkerReached);

        reader.ResetForRestart();
        Assert.False(reader.MarkerReached);
        Assert.Equal(0x56, reader.ReadBits(8));
    }

    [Fact]
    public void FillBytes_BeforeMarkerAreSkipped()
    {
        byte[] data = [0x10, 0xFF, 0xFF, 0xFF, 0xD9];
        var reader = new BitReader(data);
        Assert.Equal(0x10, reader.ReadBits(8));
        reader.ReadBits(8);
        Assert.True(reader.MarkerReached);
        Assert.Equal(0xD9, reader.Marker);
    }

    [Theory]
    [InlineData(0, 1, -1)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 2, -3)]
    [InlineData(1, 2, -2)]
    [InlineData(2, 2, 2)]
    [InlineData(3, 2, 3)]
    [InlineData(0, 3, -7)]
    [InlineData(7, 3, 7)]
    public void Extend_ProducesSignedDiff(int received, int category, int expected)
    {
        Assert.Equal(expected, BitReader.Extend(received, category));
    }

    [Fact]
    public void AlignToByte_DiscardsPartialBits()
    {
        byte[] data = [0b1010_1111, 0b0011_0000];
        var reader = new BitReader(data);
        Assert.Equal(0b1010, reader.ReadBits(4));
        reader.AlignToByte();
        Assert.Equal(0b0011_0000, reader.ReadBits(8));
    }

    [Fact]
    public void Writer_WriteBits_RoundTripsThroughReader()
    {
        (int value, int count)[] sequence =
        [
            (0b101, 3), (0b1, 1), (0xAB, 8), (0b110010, 6), (0x1FF, 9), (0, 4)
        ];

        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        foreach (var (value, count) in sequence)
            writer.WriteBits(value, count);
        writer.Flush();

        var reader = new BitReader(ms.ToArray());
        foreach (var (value, count) in sequence)
            Assert.Equal(value & ((1 << count) - 1), reader.ReadBits(count));
    }

    [Fact]
    public void Writer_StuffsZeroAfterFfByte()
    {
        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        writer.WriteBits(0xFF, 8);
        writer.Flush(); // pads remaining 0 bits (already aligned) -> nothing

        byte[] output = ms.ToArray();
        Assert.Equal([0xFF, 0x00], output);
    }

    [Fact]
    public void Writer_Flush_PadsWithOneBits()
    {
        using var ms = new MemoryStream();
        var writer = new BitWriter(ms);
        writer.WriteBits(0b101, 3);
        writer.Flush();

        byte[] output = ms.ToArray();
        // 101 followed by five 1-bit pads => 1011_1111 = 0xBF
        Assert.Single(output);
        Assert.Equal(0xBF, output[0]);
    }

    [Fact]
    public void Writer_IsDeterministic()
    {
        static byte[] Encode()
        {
            using var ms = new MemoryStream();
            var writer = new BitWriter(ms);
            for (var i = 0; i < 100; i++)
                writer.WriteBits(i, 7);
            writer.Flush();
            return ms.ToArray();
        }

        Assert.Equal(Encode(), Encode());
    }
}
