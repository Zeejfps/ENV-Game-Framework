namespace ZGF.Gui;

public sealed class AppClipboard : IClipboard
{
    private string? _text;
    
    public void SetText(string text)
    {
        _text = text;
    }

    public string? GetText()
    {
        return _text;
    }
}