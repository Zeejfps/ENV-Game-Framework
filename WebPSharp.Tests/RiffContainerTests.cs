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

    // T1: A declared RIFF size smaller than 4 cannot even contain the 'WEBP' form type and must be
    // rejected. Bytes are built by hand because the writer would never emit such a size.
    [Fact]
    public void Reader_RiffSizeBelowFour_Throws()
    {
        var bytes = new byte[12];
        WebPChunkIds.Riff.Write(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), 3); // < 4
        WebPChunkIds.WebP.Write(bytes.AsSpan(8, 4));
        Assert.Throws<WebPFormatException>(() => RiffReader.Create(bytes));
    }

    // T3: The reader stops at the declared RIFF size, so bytes appended past that boundary are
    // ignored. Here the trailing garbage even looks like a second chunk; it must not be seen.
    [Fact]
    public void Reader_TrailingBytesBeyondRiffSize_AreIgnored()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var valid = WebPTestData.Container(WebPChunkIds.Vp8L, payload);

        // Append a whole extra (fake) chunk past the declared RIFF size boundary.
        var trailing = new byte[valid.Length + 8 + 4];
        valid.CopyTo(trailing, 0);
        WebPChunkIds.Exif.Write(trailing.AsSpan(valid.Length, 4));
        BinaryPrimitives.WriteUInt32LittleEndian(trailing.AsSpan(valid.Length + 4, 4), 4);
        // (remaining 4 payload bytes stay zero)

        var reader = RiffReader.Create(trailing);
        var chunks = new List<RiffChunk>();
        while (reader.MoveNext())
            chunks.Add(reader.Current);

        Assert.Single(chunks);
        Assert.Equal(WebPChunkIds.Vp8L, chunks[0].Id);
        Assert.Equal(payload, chunks[0].Payload.ToArray());
    }

    // T4: A chunk with a zero-length payload is legal; the reader must accept it and advance to the
    // following chunk.
    [Fact]
    public void Reader_ZeroSizeChunk_ParsesAndContinues()
    {
        var real = new byte[] { 7, 7, 7, 7 };
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Iccp, Array.Empty<byte>()),
            (WebPChunkIds.Vp8L, real));

        var reader = RiffReader.Create(bytes);
        var chunks = new List<RiffChunk>();
        while (reader.MoveNext())
            chunks.Add(reader.Current);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(WebPChunkIds.Iccp, chunks[0].Id);
        Assert.Equal(0, chunks[0].Payload.Length);
        Assert.Equal(WebPChunkIds.Vp8L, chunks[1].Id);
        Assert.Equal(real, chunks[1].Payload.ToArray());
    }

    // T5: An odd-sized chunk in the MIDDLE of the container is followed by a pad byte, which the
    // reader must skip so the next chunk is read at the correct offset.
    [Fact]
    public void Reader_InteriorOddChunk_SkipsPadByte_NextChunkAtCorrectOffset()
    {
        var oddA = new byte[] { 1, 2, 3 };       // odd -> pad byte emitted after it
        var b = new byte[] { 10, 20, 30, 40 };
        var bytes = WebPTestData.Container(
            (WebPChunkIds.Iccp, oddA),
            (WebPChunkIds.Exif, b));

        var reader = RiffReader.Create(bytes);
        var chunks = new List<RiffChunk>();
        while (reader.MoveNext())
            chunks.Add(reader.Current);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(WebPChunkIds.Iccp, chunks[0].Id);
        Assert.Equal(oddA, chunks[0].Payload.ToArray());
        // The pad byte after A must have been skipped, so B is read intact.
        Assert.Equal(WebPChunkIds.Exif, chunks[1].Id);
        Assert.Equal(b, chunks[1].Payload.ToArray());
    }

    // T6: A trailing odd-sized chunk whose final pad byte is missing at end of file is tolerated
    // (spec-lenient). Confirmed behavior: RiffReader parses it rather than throwing. Bytes are built
    // by hand because the writer always emits the pad byte.
    [Fact]
    public void Reader_TrailingOddChunk_MissingPadByte_IsTolerated()
    {
        var payload = new byte[] { 0xAA, 0xBB, 0xCC }; // size 3 (odd)
        // RIFF header (12) + chunk header (8) + payload (3), NO trailing pad byte.
        var bytes = new byte[12 + 8 + payload.Length];
        WebPChunkIds.Riff.Write(bytes);
        WebPChunkIds.WebP.Write(bytes.AsSpan(8, 4));
        // RIFF size = 'WEBP' (4) + chunk header (8) + payload (3) = 15, with no pad accounted for.
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)(4 + 8 + payload.Length));
        WebPChunkIds.Exif.Write(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), (uint)payload.Length);
        payload.CopyTo(bytes, 20);

        var reader = RiffReader.Create(bytes);
        var chunks = new List<RiffChunk>();
        while (reader.MoveNext())
            chunks.Add(reader.Current);

        Assert.Single(chunks);
        Assert.Equal(WebPChunkIds.Exif, chunks[0].Id);
        Assert.Equal(payload, chunks[0].Payload.ToArray());
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
