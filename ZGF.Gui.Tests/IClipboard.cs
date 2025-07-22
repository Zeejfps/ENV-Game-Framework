namespace ZGF.Gui.Tests;

public interface IClipboard
{
    void SetText(string text);
    string? GetText();
}