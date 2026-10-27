using System.Text;
using ZGF.Gui.Hosting;

namespace ZGF.Gui.Tests;

public class WindowsHtmlFormatTests
{
    [Fact]
    public void The_header_offsets_mark_the_document_and_the_fragment_in_utf8_bytes()
    {
        const string fragment = "<span style=\"color: #569cd6;\">public</span> ünïcödé ✓";

        var bytes = WindowsHtmlFormat.Encode(fragment);

        Assert.Equal(0, bytes[^1]);
        var text = Encoding.UTF8.GetString(bytes, 0, bytes.Length - 1);
        var startHtml = Offset(text, "StartHTML");
        var endHtml = Offset(text, "EndHTML");
        var startFragment = Offset(text, "StartFragment");
        var endFragment = Offset(text, "EndFragment");
        Assert.Equal("<html>", Encoding.UTF8.GetString(bytes, startHtml, 6));
        Assert.Equal(bytes.Length - 1, endHtml);
        Assert.Equal(fragment, Encoding.UTF8.GetString(bytes, startFragment, endFragment - startFragment));
    }

    private static int Offset(string text, string name)
    {
        var line = text
            .Split("\r\n")
            .First(l => l.StartsWith(name + ":", StringComparison.Ordinal));
        return int.Parse(line.Substring(name.Length + 1));
    }
}
