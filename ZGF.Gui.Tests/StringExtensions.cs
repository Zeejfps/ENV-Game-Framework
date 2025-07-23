namespace ZGF.Gui.Tests;

public ref struct CodePointEnumerator
{
    private readonly ReadOnlySpan<char> _span;
    
    private int _index;
    public int Current { get; private set; }

    public CodePointEnumerator(ReadOnlySpan<char> span)
    {
        _span = span;
        _index = 0;
        Current = 0;
    }

    public bool MoveNext()
    {
        if (_index >= _span.Length)
            return false;

        var c = _span[_index];

        if (char.IsHighSurrogate(c) && _index + 1 < _span.Length && char.IsLowSurrogate(_span[_index + 1]))
        {
            Current = char.ConvertToUtf32(c, _span[_index + 1]);
            _index += 2;
        }
        else
        {
            Current = c;
            _index++;
        }

        return true;
    }

    public CodePointEnumerator GetEnumerator()
    {
        return this;
    }
}

public static class StringExtensions
{
    public static CodePointEnumerator EnumerateCodePoints(this ReadOnlySpan<char> s)
    {
        return new CodePointEnumerator(s);
    }
    
    public static CodePointEnumerator EnumerateCodePoints(this string s)
    {
        return new CodePointEnumerator(s);
    }
}