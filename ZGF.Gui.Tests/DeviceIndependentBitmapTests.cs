using System.Buffers.Binary;
using ZGF.Gui.Hosting;

namespace ZGF.Gui.Tests;

public class DeviceIndependentBitmapTests
{
    private const uint Uncompressed = 0;
    private const uint Bitfields = 3;

    [Fact]
    public void A_bottom_up_24_bit_bitmap_reads_top_row_first_with_its_rows_unpadded()
    {
        // 1x2, so each three-byte row is padded to four. Bottom row blue, top row red, as BGR.
        var dib = Bitmap(40, 1, 2, 24, Uncompressed, [], [0xFF, 0, 0, 0, 0, 0, 0xFF, 0]);

        var image = DeviceIndependentBitmap.Decode(dib)!;

        Assert.Equal((1, 2), (image.Width, image.Height));
        Assert.Equal(new byte[] { 0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF }, image.Rgba);
    }

    [Fact]
    public void A_top_down_32_bit_bitmap_keeps_its_row_order()
    {
        var dib = Bitmap(40, 1, -2, 32, Uncompressed, [], [0, 0, 0xFF, 0x80, 0xFF, 0, 0, 0x40]);

        var image = DeviceIndependentBitmap.Decode(dib)!;

        Assert.Equal(new byte[] { 0xFF, 0, 0, 0x80, 0, 0, 0xFF, 0x40 }, image.Rgba);
    }

    [Fact]
    public void A_32_bit_bitmap_whose_fourth_bytes_are_all_zero_is_opaque()
    {
        var dib = Bitmap(40, 2, 1, 32, Uncompressed, [], [1, 2, 3, 0, 4, 5, 6, 0]);

        var image = DeviceIndependentBitmap.Decode(dib)!;

        Assert.Equal(new byte[] { 3, 2, 1, 0xFF, 6, 5, 4, 0xFF }, image.Rgba);
    }

    [Fact]
    public void A_bitfields_bitmap_after_an_info_header_takes_its_masks_from_after_it()
    {
        // Masks R, G, B at the low, middle and high bytes: the reverse of the usual order.
        var masks = Masks(0x000000FF, 0x0000FF00, 0x00FF0000);
        var dib = Bitmap(40, 1, 1, 32, Bitfields, masks, [10, 20, 30, 0]);

        var image = DeviceIndependentBitmap.Decode(dib)!;

        Assert.Equal(new byte[] { 10, 20, 30, 0xFF }, image.Rgba);
    }

    [Fact]
    public void A_v5_bitmap_takes_its_masks_and_alpha_from_its_header()
    {
        var dib = Bitmap(124, 1, 1, 32, Bitfields, [], [10, 20, 30, 40]);
        Masks(0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000).CopyTo(dib.AsSpan(40));

        var image = DeviceIndependentBitmap.Decode(dib)!;

        Assert.Equal(new byte[] { 30, 20, 10, 40 }, image.Rgba);
    }

    [Theory]
    [InlineData(8, Uncompressed)]
    [InlineData(24, Bitfields)]
    [InlineData(32, 1u)]
    public void A_form_it_does_not_read_is_no_picture(int bitCount, uint compression)
    {
        var dib = Bitmap(40, 1, 1, (ushort)bitCount, compression, [], new byte[16]);

        Assert.Null(DeviceIndependentBitmap.Decode(dib));
    }

    [Fact]
    public void A_bitmap_cut_short_is_no_picture()
    {
        var dib = Bitmap(40, 4, 4, 32, Uncompressed, [], new byte[4]);

        Assert.Null(DeviceIndependentBitmap.Decode(dib));
    }

    private static byte[] Bitmap(int headerSize, int width, int height, ushort bitCount, uint compression, byte[] afterHeader, byte[] pixels)
    {
        var dib = new byte[headerSize + afterHeader.Length + pixels.Length];
        BinaryPrimitives.WriteInt32LittleEndian(dib, headerSize);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4), width);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8), height);
        BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(dib.AsSpan(14), bitCount);
        BinaryPrimitives.WriteUInt32LittleEndian(dib.AsSpan(16), compression);
        afterHeader.CopyTo(dib.AsSpan(headerSize));
        pixels.CopyTo(dib.AsSpan(headerSize + afterHeader.Length));
        return dib;
    }

    private static byte[] Masks(params uint[] masks)
    {
        var bytes = new byte[masks.Length * 4];
        for (var i = 0; i < masks.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), masks[i]);
        return bytes;
    }
}
