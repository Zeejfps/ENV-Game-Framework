using WebPSharp.Vp8L;

namespace WebPSharp.Tests;

public class Vp8LBitStreamTests
{
    [Fact]
    public void ReadBits_LsbFirstWithinByte()
    {
        // Byte 0b1010_0101 = 0xA5. LSB-first reads should peel bits 1,0,1,0,0,1,0,1.
        var reader = new Vp8LBitReader(new byte[] { 0xA5 });
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(0u, reader.ReadBits(1));
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(0u, reader.ReadBits(1));
        Assert.Equal(0u, reader.ReadBits(1));
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(0u, reader.ReadBits(1));
        Assert.Equal(1u, reader.ReadBits(1));
    }

    [Fact]
    public void ReadBits_MultiBitValue()
    {
        // 0xA5 read as a single 8-bit value returns 0xA5.
        var reader = new Vp8LBitReader(new byte[] { 0xA5 });
        Assert.Equal(0xA5u, reader.ReadBits(8));
    }

    [Fact]
    public void ReadBits_CrossesByteBoundary_LittleEndian()
    {
        // Two bytes 0x34, 0x12 read as a 16-bit value give 0x1234 (little-endian).
        var reader = new Vp8LBitReader(new byte[] { 0x34, 0x12 });
        Assert.Equal(0x1234u, reader.ReadBits(16));
    }

    [Fact]
    public void ReadBits_TwentyFourBits()
    {
        var reader = new Vp8LBitReader(new byte[] { 0x78, 0x56, 0x34 });
        Assert.Equal(0x345678u, reader.ReadBits(24));
    }

    [Fact]
    public void ReadBits_ThirtyTwoBits()
    {
        var reader = new Vp8LBitReader(new byte[] { 0xEF, 0xBE, 0xAD, 0xDE });
        Assert.Equal(0xDEADBEEFu, reader.ReadBits(32));
    }

    [Fact]
    public void ReadBits_ZeroWidth_ReturnsZero()
    {
        var reader = new Vp8LBitReader(new byte[] { 0xFF });
        Assert.Equal(0u, reader.ReadBits(0));
        Assert.Equal(0xFFu, reader.ReadBits(8));
    }

    [Fact]
    public void ReadBits_PastEnd_SetsEndOfStream()
    {
        var reader = new Vp8LBitReader(new byte[] { 0xFF });
        Assert.False(reader.IsEndOfStream);
        Assert.Equal(0xFFu, reader.ReadBits(8));
        Assert.False(reader.IsEndOfStream);
        reader.ReadBits(1); // one bit past the end
        Assert.True(reader.IsEndOfStream);
    }

    [Fact]
    public void Writer_Then_Reader_RoundTripsSingleValues()
    {
        var writer = new Vp8LBitWriter();
        writer.PutBits(0b101, 3);
        writer.PutBits(0x1234, 16);
        writer.PutBits(1, 1);
        var bytes = writer.ToArray();

        var reader = new Vp8LBitReader(bytes);
        Assert.Equal(0b101u, reader.ReadBits(3));
        Assert.Equal(0x1234u, reader.ReadBits(16));
        Assert.Equal(1u, reader.ReadBits(1));
    }

    [Fact]
    public void Writer_LsbFirst_ProducesExpectedBytes()
    {
        var writer = new Vp8LBitWriter();
        // Write bits 1,0,1,0,0,1,0,1 (LSB first) -> byte 0xA5.
        writer.PutBits(1, 1);
        writer.PutBits(0, 1);
        writer.PutBits(1, 1);
        writer.PutBits(0, 1);
        writer.PutBits(0, 1);
        writer.PutBits(1, 1);
        writer.PutBits(0, 1);
        writer.PutBits(1, 1);
        Assert.Equal(new byte[] { 0xA5 }, writer.ToArray());
    }

    [Fact]
    public void Writer_PartialFinalByte_ZeroPadded()
    {
        var writer = new Vp8LBitWriter();
        writer.PutBits(0b111, 3);
        Assert.Equal(new byte[] { 0b0000_0111 }, writer.ToArray());
    }

    [Fact]
    public void Writer_TracksBitLength()
    {
        var writer = new Vp8LBitWriter();
        writer.PutBits(0, 5);
        writer.PutBits(0, 4);
        Assert.Equal(9, writer.BitLength);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(12345)]
    public void RoundTrip_DeterministicSequence(int seed)
    {
        var rng = new Random(seed);
        var values = new List<(uint Value, int Width)>();
        var writer = new Vp8LBitWriter();
        for (var i = 0; i < 500; i++)
        {
            var width = rng.Next(1, 25);
            var value = (uint)rng.NextInt64(0, 1L << width);
            values.Add((value, width));
            writer.PutBits(value, width);
        }

        var reader = new Vp8LBitReader(writer.ToArray());
        foreach (var (value, width) in values)
            Assert.Equal(value, reader.ReadBits(width));
        Assert.False(reader.IsEndOfStream);
    }
}
