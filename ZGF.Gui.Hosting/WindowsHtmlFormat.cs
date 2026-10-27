using System.Globalization;
using System.Text;

namespace ZGF.Gui.Hosting;

/// <summary>The "HTML Format" Windows keeps HTML on the clipboard in: a header of UTF-8 byte offsets
/// into the document that follows it, which marks where the copied fragment starts and ends.</summary>
internal static class WindowsHtmlFormat
{
    public const string Name = "HTML Format";

    private const string Header =
        "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
    private const string DocumentStart = "<html><body>\r\n<!--StartFragment-->";
    private const string DocumentEnd = "<!--EndFragment-->\r\n</body></html>";

    /// <summary>The clipboard bytes of <paramref name="fragment"/>, ending in a null.</summary>
    public static byte[] Encode(string fragment)
    {
        var startHtml = Encoding.UTF8.GetByteCount(FormatHeader(0, 0, 0, 0));
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(DocumentStart);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(DocumentEnd);
        var header = FormatHeader(startHtml, endHtml, startFragment, endFragment);
        return Encoding.UTF8.GetBytes(header + DocumentStart + fragment + DocumentEnd + "\0");
    }

    private static string FormatHeader(int startHtml, int endHtml, int startFragment, int endFragment) =>
        string.Format(CultureInfo.InvariantCulture, Header, startHtml, endHtml, startFragment, endFragment);
}
