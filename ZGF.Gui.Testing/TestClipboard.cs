namespace ZGF.Gui.Testing;

public sealed class TestClipboard : IClipboard
{
    public string? Text { get; set; }

    /// <summary>The HTML fragment last put on the clipboard, null after plain text.</summary>
    public string? Html { get; set; }

    public void SetText(string text)
    {
        Text = text;
        Html = null;
    }

    public void SetHtml(string html, string text)
    {
        Text = text;
        Html = html;
    }

    public string? GetText() => Text;

    /// <summary>The picture on the clipboard, as a screenshot would put it there.</summary>
    public ClipboardImage? Image { get; set; }

    public ClipboardImage? GetImage() => Image;
}
