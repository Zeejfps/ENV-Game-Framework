namespace ZGF.Gui.Testing;

public sealed class TestClipboard : IClipboard
{
    public string? Text { get; set; }

    public void SetText(string text) => Text = text;

    public string? GetText() => Text;
}
