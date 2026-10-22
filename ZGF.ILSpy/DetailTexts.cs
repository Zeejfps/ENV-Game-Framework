using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>A decoded text under a metadata grid's row, as a code view shows it.</summary>
internal sealed record DetailText(DecompiledDocument Document, CodeViewState View);

/// <summary>How many lines a text has, and how many characters its longest one.</summary>
internal readonly record struct TextSize(int Lines, int Longest)
{
    public static TextSize Of(string text)
    {
        var lines = text.Split('\n');
        var longest = lines.Max(static l => l.TrimEnd('\r').Length);
        return new TextSize(lines.Length, longest);
    }
}

/// <summary>The decoded texts under the rows of the metadata grid shown, each measured and turned into
/// a document once.</summary>
internal sealed class DetailTexts
{
    private readonly Dictionary<int, DetailText> _texts = new();
    private readonly Dictionary<int, TextSize> _sizes = new();

    public DetailText Get(int row, string title, string text)
    {
        if (_texts.TryGetValue(row, out var shown))
            return shown;
        var document = DecompiledDocument.FromText(title, text);
        shown = new DetailText(document, new CodeViewState());
        _texts[row] = shown;
        return shown;
    }

    public TextSize SizeOf(int row, string text)
    {
        if (_sizes.TryGetValue(row, out var size))
            return size;
        size = TextSize.Of(text);
        _sizes[row] = size;
        return size;
    }

    public void Clear()
    {
        _texts.Clear();
        _sizes.Clear();
    }
}
