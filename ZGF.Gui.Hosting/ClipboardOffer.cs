using System.Text;

namespace ZGF.Gui.Hosting;

/// <summary>What the app last put on the clipboard, encoded the ways other apps ask for it.</summary>
internal sealed class ClipboardOffer(string text, string? html)
{
    public string Text { get; } = text;
    public byte[] Utf8Text { get; } = Encoding.UTF8.GetBytes(text);
    public byte[] Latin1Text { get; } = Encoding.Latin1.GetBytes(text);
    public byte[]? Html { get; } = EncodeHtml(html);

    private static byte[]? EncodeHtml(string? html)
    {
        if (html == null)
            return null;
        return Encoding.UTF8.GetBytes(HtmlDocument.WithCharset(html));
    }
}
