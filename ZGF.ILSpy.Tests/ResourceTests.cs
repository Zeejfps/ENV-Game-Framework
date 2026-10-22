using ZGF.ILSpy.Core;
using ZGF.ILSpy.Core.Metadata;
using ZGF.ILSpy.Core.Resources;

namespace ZGF.ILSpy.Tests;

public sealed class ResourceTests
{
    private static BrowseNode Resources(LoadedAssembly assembly) => assembly.Root.Child("Resources");

    private static byte[] Fixture(string name)
    {
        using var stream = typeof(ResourceTests).Assembly.GetManifestResourceStream(name)!;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    /// <summary>The pixels the fixture pictures hold, top-down: red, green / blue, half-transparent white.</summary>
    private static readonly byte[] Expected = [255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 128];

    [Theory]
    [InlineData("logo.png", "PNG")]
    [InlineData("logo.ico", "ICO")]
    [InlineData("pointer.cur", "CUR")]
    public void Pictures_decode_to_their_pixels(string name, string format)
    {
        var image = ResourceImage.Decode(Fixture(name));

        Assert.Equal((2, 2, format), (image.Width, image.Height, image.Format));
        Assert.Equal(Expected, image.Rgba);
    }

    [Fact]
    public void A_bitmap_without_alpha_is_opaque()
    {
        var image = ResourceImage.Decode(Fixture("logo.bmp"));

        Assert.Equal((2, 2, "BMP"), (image.Width, image.Height, image.Format));
        Assert.Equal(Expected[..12], image.Rgba[..12]);
        Assert.Equal([255, 255, 255, 255], image.Rgba[12..]);
    }

    [Fact]
    public void A_gif_decodes_its_first_frame()
    {
        var image = ResourceImage.Decode(Fixture("red.gif"));

        Assert.Equal((1, 1, "GIF"), (image.Width, image.Height, image.Format));
        Assert.Equal([255, 0, 0, 255], image.Rgba);
    }

    [Fact]
    public void A_jpeg_decodes_through_JpegSharp()
    {
        var pixels = Enumerable.Repeat(unchecked((int)0x2040A0FF), 16 * 16).ToArray();
        var data = JpegSharp.Api.Jpeg.Encode(JpegSharp.Api.JpegImage.CreateFromRgba8888(16, 16, pixels));

        var image = ResourceImage.Decode(data);

        Assert.Equal((16, 16, "JPEG"), (image.Width, image.Height, image.Format));
        Assert.InRange(image.Rgba[2], 0x90, 0xB0);
        Assert.Equal(255, image.Rgba[3]);
    }

    [Fact]
    public void Anything_else_is_no_picture()
    {
        Assert.Throws<InvalidDataException>(() => ResourceImage.Decode(Fixture("blob.bin")));
        Assert.Throws<InvalidDataException>(() => ResourceImage.Decode([0x89, (byte)'P', (byte)'N', (byte)'G', 1, 2]));
    }

    [Fact]
    public void Pictures_show_as_pictures_and_the_rest_as_documents()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var resources = Resources(assembly);

        Assert.Equal(NodeKind.ResourceImage, resources.Child("logo.png").Kind);
        Assert.IsType<ImageContent>(resources.Child("red.gif").Content());
        Assert.False(resources.Child("red.gif").DependsOnLanguage);
        Assert.IsType<DocumentContent>(resources.Child("sample.xml").Content());
        Assert.True(resources.Child("sample.xml").DependsOnLanguage);
        Assert.Equal(NodeKind.ResourcesFile, resources.Child("Mixed.resources").Kind);
        Assert.All(resources.Children, c => Assert.True(c.HasResource));

        var image = Assert.IsType<ImageContent>(resources.Child("logo.png").Content()).Image;
        Assert.Equal(Expected, image.Rgba);
        Assert.Equal(Fixture("logo.png"), resources.Child("logo.png").ReadResource());
    }

    [Fact]
    public void A_picture_that_cannot_be_read_says_so()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var broken = Resources(assembly).Child("broken.png");

        var document = Assert.IsType<DocumentContent>(broken.Content(DecompilationLanguage.IL)).Document;

        Assert.False(broken.DependsOnLanguage);
        Assert.Equal("// This is no PNG, JPEG, BMP, GIF, ICO or CUR picture.", document.Lines[0].Text);
        Assert.Equal(SpanKind.Comment, document.Lines[0].Spans[0].Kind);
        Assert.Contains("not a picture", document.GetText());
    }

    [Fact]
    public void Xml_is_colored_as_markup()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var document = Resources(assembly).Child("sample.xml").Decompile(DecompilationLanguage.CSharp);
        var spans = document.SpansOf().ToList();

        Assert.StartsWith("<?xml version=\"1.0\"?>", document.GetText());
        Assert.Contains(("<?xml version=\"1.0\"?>", SpanKind.Preprocessor), spans.Select(s => (s.Text, s.Span.Kind)));
        Assert.Contains(("<!-- a sample -->", SpanKind.Comment), spans.Select(s => (s.Text, s.Span.Kind)));
        Assert.Contains(("root", SpanKind.Keyword), spans.Select(s => (s.Text, s.Span.Kind)));
        Assert.Contains(("kind", SpanKind.Field), spans.Select(s => (s.Text, s.Span.Kind)));
        Assert.Contains(("\"sample\"", SpanKind.String), spans.Select(s => (s.Text, s.Span.Kind)));
    }

    [Fact]
    public void Json_is_colored_by_what_each_token_is()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var spans = Resources(assembly).Child("data.json").Decompile(DecompilationLanguage.CSharp).SpansOf().Select(s => (s.Text, s.Span.Kind)).ToList();

        Assert.Contains(("\"name\"", SpanKind.Field), spans);
        Assert.Contains(("\"fixture\"", SpanKind.String), spans);
        Assert.Contains(("2", SpanKind.Number), spans);
        Assert.Contains(("true", SpanKind.Keyword), spans);
    }

    [Fact]
    public void Binary_resources_show_as_a_hex_dump()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var text = Resources(assembly).Child("blob.bin").Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.Contains("blob.bin (Embedded, 6 bytes)", text);
        Assert.Contains("00 01 02 03 FF FE", text);
    }

    [Fact]
    public void A_resources_file_lists_its_entries_in_a_grid_and_its_byte_entries_as_nodes()
    {
        var (_, assembly) = Trees.OpenTestAssembly();
        var file = Resources(assembly).Child("Mixed.resources");

        var grid = Assert.IsType<GridContent>(file.Content()).Grid;
        var rows = Enumerable.Range(0, grid.RowCount).Select(r => (grid.Text(r, 0), grid.Text(r, 1), grid.Text(r, 2))).ToList();

        Assert.Equal(["Name", "Value", "Type"], grid.Columns.Select(c => c.Name));
        Assert.Equal(
            [
                ("Count", "42", "System.Int32"), ("Greeting", "Hello", "System.String"), ("Multiline", "first line\nsecond line", "System.String"),
                ("Notes.xml", "95 bytes", "System.Byte[]"), ("Picture.png", "76 bytes", "System.Byte[]"),
                ("Poem", "roses\nviolets\nsugar", "System.String"),
            ],
            rows);
        Assert.Equal("first line\nsecond line", grid.Tooltip(2, 1)!.Text);
        Assert.Equal(("first line", 1), grid.Lines(2, 1));
        Assert.Equal(("Hello", 0), grid.Lines(1, 1));
        Assert.False(grid.HasDetails);
        Assert.Null(grid.Tooltip(1, 1));
        Assert.Equal([("Notes.xml", NodeKind.Resource), ("Picture.png", NodeKind.ResourceImage)], file.Children.Select(c => (c.Text, c.Kind)));
        Assert.Equal(Expected, Assert.IsType<ImageContent>(file.Child("Picture.png").Content()).Image.Rgba);
        Assert.Contains(("root", SpanKind.Keyword), file.Child("Notes.xml").Decompile(DecompilationLanguage.CSharp).SpansOf().Select(s => (s.Text, s.Span.Kind)));
    }

    [Fact]
    public void The_resources_folder_describes_each_resource()
    {
        var (_, assembly) = Trees.OpenTestAssembly();

        var text = Resources(assembly).Decompile(DecompilationLanguage.CSharp).GetText();

        Assert.Contains("// logo.png (Embedded, 76 bytes)", text);
        Assert.Contains("// Mixed.resources (Embedded, ", text);
    }
}
