using ZGF.ILSpy.Core;

namespace ZGF.ILSpy;

/// <summary>The case-insensitive matches of a query in a document, found once per pair.</summary>
internal sealed class CodeFindIndex
{
    private readonly List<TextPosition> _matches = new();
    private DecompiledDocument? _document;
    private string _query = "";

    public IReadOnlyList<TextPosition> Matches => _matches;
    public string Query => _query;
    public int Length => _query.Length;

    /// <summary>Rescans when the document or query changed; returns whether it did.</summary>
    public bool Update(DecompiledDocument document, string query)
    {
        if (ReferenceEquals(document, _document) && string.Equals(query, _query, StringComparison.Ordinal))
            return false;
        _document = document;
        _query = query;
        _matches.Clear();
        if (query.Length == 0) return true;

        var lines = document.Lines;
        for (var line = 0; line < lines.Length; line++)
        {
            var text = lines[line].Text;
            var at = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                _matches.Add(new TextPosition(line, at));
                at = text.IndexOf(query, at + query.Length, StringComparison.OrdinalIgnoreCase);
            }
        }
        return true;
    }

    /// <summary>The first match at or after <paramref name="at"/>, wrapping to the first; -1 when there are none.</summary>
    public int FirstFrom(TextPosition at)
    {
        if (_matches.Count == 0) return -1;
        var index = FirstOnOrAfter(at);
        return index < _matches.Count ? index : 0;
    }

    /// <summary>The index of the first match at or after <paramref name="at"/>, or the count.</summary>
    public int FirstOnOrAfter(TextPosition at)
    {
        int low = 0, high = _matches.Count;
        while (low < high)
        {
            var mid = (low + high) >> 1;
            if (_matches[mid] < at) low = mid + 1;
            else high = mid;
        }
        return low;
    }
}
