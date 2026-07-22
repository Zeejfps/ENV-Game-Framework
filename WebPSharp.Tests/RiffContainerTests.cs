using System.Buffers.Binary;
using WebPSharp.Api.Exceptions;
using WebPSharp.Container;

namespace WebPSharp.Tests;

public class RiffContainerTests
{
    [Fact]
    public void Writer_Then_Reader_RoundTripsChunks()
    {
        var a = new byte[] { 1, 2, 3, 4 };
        var b = new byte[] { 9, 8, 7 }; // odd length -> exercises padding
        var bytes = WebPTestData.Container((WebPChunkIds.Iccp, a), (WebPChunkIds.Exif, b));

        var reader = RiffReader.Create(bytes);
        var chunks = new List<RiffChunk>();
        while (reader.MoveNext())
            chunks.Add(reader.Current);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(WebPChunkIds.Iccp, chunks[0].Id);
        Assert.Equal(a, chunks[0].Payload.ToArray());
        Assert.Equal(WebPChunkIds.Exif, chunks[1].Id);
        Assert.Equal(b, chunks[1].Payload.ToArray());
    }

    [Fact]
    public void Writer_OddPayload_EmitsPadByte_ForEvenTotal()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Xmp, new byte[] { 42 });
        // 12 (RIFF/WEBP) + 8 (chunk header) + 1 (payload) + 1 (pad) = 22
        Assert.Equal(22, bytes.Length);
        Assert.Equal(0, bytes[^1]); // pad byte is zero
    }

    [Fact]
    public void Writer_PatchesRiffSizeField()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        var declared = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4));
        Assert.Equal((uint)(bytes.Length - 8), declared);
    }

    [Fact]
    public void Reader_ExposesRiffSize()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        var reader = RiffReader.Create(bytes);
        Assert.Equal(bytes.Length - 8, reader.RiffSize);
    }

    [Fact]
    public void Reader_TooShort_Throws()
    {
        Assert.Throws<WebPFormatException>(() => RiffReader.Create(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void Reader_BadRiffSignature_Throws()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        bytes[0] = (byte)'X';
        var ex = Assert.Throws<WebPFormatException>(() => RiffReader.Create(bytes));
        Assert.Contains("RIFF", ex.Message);
    }

    [Fact]
    public void Reader_BadWebPForm_Throws()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        bytes[8] = (byte)'X';
        var ex = Assert.Throws<WebPFormatException>(() => RiffReader.Create(bytes));
        Assert.Contains("WEBP", ex.Message);
    }

    [Fact]
    public void Reader_ChunkSizeExceedsBuffer_Throws()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        // Inflate the first chunk's size field (at offset 16) far beyond the buffer.
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), 0xFFFF);
        var reader = RiffReader.Create(bytes);
        Assert.Throws<WebPFormatException>(() =>
        {
            while (reader.MoveNext()) { }
        });
    }

    [Fact]
    public void Reader_RiffSizeExceedsBuffer_Throws()
    {
        var bytes = WebPTestData.Container(WebPChunkIds.Vp8L, new byte[] { 1, 2, 3, 4 });
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), 0xFFFFFF);
        Assert.Throws<WebPFormatException>(() => RiffReader.Create(bytes));
    }

    [Fact]
    public void Reader_EmptyContainer_YieldsNoChunks()
    {
        // A bare RIFF/WEBP header with no chunks.
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.Complete();
        var reader = RiffReader.Create(ms.ToArray());
        Assert.False(reader.MoveNext());
    }

    [Fact]
    public void Writer_CompleteTwice_Throws()
    {
        using var ms = new MemoryStream();
        var writer = new RiffWriter(ms);
        writer.Complete();
        Assert.Throws<InvalidOperationException>(() => writer.Complete());
    }

    [Fact]
    public void Writer_NonSeekableStream_Throws()
    {
        using var nonSeekable = new NonSeekableStream();
        Assert.Throws<ArgumentException>(() => new RiffWriter(nonSeekable));
    }

    private sealed class NonSeekableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
