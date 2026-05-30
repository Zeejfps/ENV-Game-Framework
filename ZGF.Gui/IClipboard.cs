namespace ZGF.Gui;

public interface IClipboard
{
    void SetText(string text);
    string? GetText();
}