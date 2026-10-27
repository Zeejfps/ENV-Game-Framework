namespace ZGF.Gui;

public interface IClipboard
{
    void SetText(string text);

    /// <summary>Puts the HTML fragment <paramref name="html"/> on the clipboard, with
    /// <paramref name="text"/> for where only plain text can be pasted.</summary>
    void SetHtml(string html, string text);

    string? GetText();

    /// <summary>The picture on the clipboard, as a screenshot puts there, or null where there is
    /// none or this clipboard cannot read one.</summary>
    ClipboardImage? GetImage();
}